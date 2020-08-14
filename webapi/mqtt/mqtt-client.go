package mqtt

import (
	"time"
	"fmt"
	"os"

	"github.com/streadway/amqp"
)
// Setup dial the RabbitMq instance, create a channel and bind an exchange to the channel
func Setup(exchangeName string) *amqp.Channel{
	connection := dial()
	channel := createChannel(connection)
	createExchange(channel, exchangeName)
	return channel
}

func dial() *amqp.Connection {
	// Get the connection string from the environment variable
	url := os.Getenv("AMQP_URL")

	//If it doesn't exist, use the default connection string.
	if url == "" {
		//Don't do this in production, this is for testing purposes only.
		url = "amqp://guest:guest@localhost:5672"
	}

	maxRetries := 20
	retries := 0

	for retries < maxRetries{		
		// Connect to the rabbitMQ instance
		connection, err := amqp.Dial(url)

		if err != nil{
			fmt.Println("Failed to establish connection with RabbitMQ, retry nr ", retries)
			retries++
			time.Sleep(1 * time.Second)
			continue
		}

		return connection
	}

	return nil
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
	var _, err = queueModel.Channel.QueueDeclare(queueModel.QueueName, true, false, false, false, nil)

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
