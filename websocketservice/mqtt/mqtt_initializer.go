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

//Consumer is the consumer struct handling the consumation
type Consumer struct {
	conn    *amqp.Connection
	channel *amqp.Channel
	queue   amqp.Queue
	err     chan error
}

const maxConnectionRetries = 20
const queueName = "forward_roundtrip"

var (
	consumerPool = make(map[string]*Consumer)
)

//NewConsumer returns the new consumer object
func NewConsumer(name string) *Consumer {
	if consumer, ok := consumerPool[name]; ok {
		return consumer
	}
	consumer := &Consumer{
		err: make(chan error),
	}
	consumerPool[name] = consumer
	return consumer
}

// Connect will start a connection to the MQTT broker and declare an exchange
func (consumer *Consumer) Connect() error {
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

		consumer.conn = amqpConnection
		break
	}

	if consumer.conn == nil {
		panic("Failed to establish connection to RabbitMQ! Will exit")
	}

	return nil
}

// DeclareQueue setup chans to listen for connection closed
// Declare a channel and declare queue
func (consumer *Consumer) DeclareQueue() error {
	var err error

	go func() {
		//Listen to NotifyClose
		<-consumer.conn.NotifyClose(make(chan *amqp.Error))
		consumer.err <- errors.New("Connection Closed")
	}()

	// Create a channel
	if consumer.channel, err = consumer.conn.Channel(); err != nil {
		return fmt.Errorf("Channel: %s", err)
	}

	queue, err := consumer.channel.QueueDeclare(
		queueName,
		false,
		false,
		false,
		false,
		nil,
	)

	if err != nil {
		return err
	}

	consumer.queue = queue
	return nil
}

// Reconnect reconnects..
func (consumer *Consumer) Reconnect() error {
	if err := consumer.Connect(); err != nil {
		return err
	}
	err := consumer.DeclareQueue()
	if err != nil {
		return err
	}
	return nil
}
