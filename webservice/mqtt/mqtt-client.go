package mqtt

import (
	"fmt"
	"os"
	"time"

	"github.com/streadway/amqp"
)

const maxConnectionRetries = 20

// Setup create a new client and connect to the RabbitMq instance, create a channel
func Setup(exchangeName string) *amqp.Channel {
	connection := dial()
	channel := createChannel(connection)
	createExchange(channel, exchangeName)
	return channel
}

func dial() *amqp.Connection {
	// Get the connection string from the environment variable
	url := os.Getenv("AMQP_URL")

	retries := 0

	for retries <= maxConnectionRetries {
		// Connect to the rabbitMQ instance
		connection, err := amqp.Dial(url)

		if err != nil {
			fmt.Printf("Failed to establish connection with RabbitMQ, retry nr %d / %d \n", retries, maxConnectionRetries)
			retries++
			time.Sleep(1 * time.Second)
			continue
		}

		return connection
	}

	panic("Failed to establish connection to RabbitMQ! Will exit")
}

func createChannel(conn *amqp.Connection) *amqp.Channel {
	// Create a channel from the connection. We'll use channels to access the data in the queue rather than the connection itself.
	channel, err := conn.Channel()

	if err != nil {
		panic("could not open RabbitMQ channel:" + err.Error())
	}

	return channel
}

func createExchange(channel *amqp.Channel, exchangeName string) {
	// We create an exchange that will bind to the queue to send and receive messages
	err := channel.ExchangeDeclare(exchangeName, "fanout", true, false, false, false, nil)

	if err != nil {
		panic(err)
	}
}

// SetupQueue set up a queue and bind it to an exchange
func SetupQueue(queueModel QueueModel) {
	// We create a queue
	var _, err = queueModel.Channel.QueueDeclare(queueModel.QueueName, false, false, false, false, nil)

	if err != nil {
		panic("error declaring the queue: " + err.Error())
	}

	// We bind the queue to the exchange to send and receive data from the queue
	err = queueModel.Channel.QueueBind(queueModel.QueueName, "#", queueModel.Exchange, false, nil)

	if err != nil {
		panic("error binding to the queue: " + err.Error())
	}
}

// Publish will publish a message to an exchange
func Publish(publishModel PublishModel) {
	// We publish the message to the exchange we created earlier
	err := publishModel.Channel.Publish(publishModel.Exchange, "", false, false, publishModel.Message)

	if err != nil {
		panic("error publishing a message to the queue:" + err.Error())
	}

}
