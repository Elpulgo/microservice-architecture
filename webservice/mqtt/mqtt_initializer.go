package mqtt

import (
	"errors"
	"fmt"
	"log"
	"os"
	"time"

	"github.com/streadway/amqp"
)

//MessageBody is the struct for the body passed in the AMQP message. The type will be set on the Request header
type MessageBody struct {
	Data []byte
	Type string
}

//Message is the amqp request to publish
type Message struct {
	Queue         string
	ReplyTo       string
	ContentType   string
	CorrelationID string
	Priority      uint8
	Body          MessageBody
}

//Connection is the connection created
type Connection struct {
	name         string
	conn         *amqp.Connection
	channel      *amqp.Channel
	exchange     string
	Queues       []string
	err          chan error
	confirmation chan amqp.Confirmation
}

const maxConnectionRetries = 20

var (
	connectionPool = make(map[string]*Connection)
)

//NewConnection returns the new connection object
func NewConnection(name string, exchange string, queues []string) *Connection {
	if connection, ok := connectionPool[name]; ok {
		return connection
	}
	connection := &Connection{
		exchange:     exchange,
		Queues:       queues,
		err:          make(chan error),
		confirmation: make(chan amqp.Confirmation, 1),
	}
	connectionPool[name] = connection
	return connection
}

// Connect will start a connection to the MQTT broker and declare an exchange
func (connection *Connection) Connect() error {
	url := os.Getenv("AMQP_URL")

	retries := 0

	for retries <= maxConnectionRetries {
		// Connect to the rabbitMQ instance
		amqpConnection, err := amqp.Dial(url)

		if err != nil {
			log.Printf("Failed to establish connection with RabbitMQ, retry nr %d / %d \n", retries, maxConnectionRetries)
			retries++
			time.Sleep(1 * time.Second)
			continue
		}

		connection.conn = amqpConnection
		break
	}

	if connection.conn == nil {
		panic("Failed to establish connection to RabbitMQ! Will exit")
	}

	return nil
}

// DeclareChannel setup chans to listen for connection closed and confirmation.
// Declare a channel, set in confirm mode and declare exchange
func (connection *Connection) DeclareChannel() error {
	var err error

	go func() {
		//Listen to NotifyClose
		<-connection.conn.NotifyClose(make(chan *amqp.Error))
		connection.err <- errors.New("Connection Closed")
	}()

	// Create a channel
	if connection.channel, err = connection.conn.Channel(); err != nil {
		return fmt.Errorf("Channel: %s", err)
	}

	// Set channel in confirm mode
	if err := connection.channel.Confirm(false); err != nil {
		return fmt.Errorf("Channel couldn't be set in confirm mode: %s", err)
	}

	// Setup channel for confirmation
	go func() {
		connection.confirmation = connection.channel.NotifyPublish(make(chan amqp.Confirmation, 1))
	}()

	if err := connection.channel.ExchangeDeclare(
		connection.exchange, // name
		"direct",            // type
		false,               // durable
		false,               // auto-deleted
		false,               // internal
		false,               // noWait
		nil,                 // arguments
	); err != nil {
		return fmt.Errorf("Error in Exchange Declare: %s", err)
	}
	return nil
}

// BindQueues will bind the queues to the exchange
func (connection *Connection) BindQueues() error {
	for _, queue := range connection.Queues {
		if _, err := connection.channel.QueueDeclare(
			queue, // queue name
			false, // durable
			false, // auto-delete
			false, // exclusive
			false, // noWait
			nil,   // arguments
		); err != nil {
			return fmt.Errorf("error in declaring the queue %s", err)
		}

		if err := connection.channel.QueueBind(
			queue,               // queue name
			queue,               // routing key
			connection.exchange, // exchange name
			false,               // noWait
			nil,                 // arguments
		); err != nil {
			return fmt.Errorf("Queue  Bind error: %s", err)
		}
	}

	return nil
}

// Reconnect reconnects..
func (connection *Connection) Reconnect() error {
	if err := connection.Connect(); err != nil {
		return err
	}
	if err := connection.BindQueues(); err != nil {
		return err
	}
	return nil
}
