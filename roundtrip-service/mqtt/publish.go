package mqtt

import (
	"fmt"
	"log"

	"github.com/streadway/amqp"
)

// Publish will publish message to MQTT broker and wait for confirmation
func (connection *Connection) Publish(message amqp.Publishing, key string) error {
	select {
	case err := <-connection.err:
		if err != nil {
			log.Println("Will reconnect...")
			connection.Reconnect()
		}
	default:
	}

	if err := connection.channel.Publish(
		connection.exchange, // exchange name
		key,                 // key
		false,               // mandatory
		false,               // immediate
		message,             // message
	); err != nil {
		return fmt.Errorf("Error in Publishing: %s", err)
	}

	confirmation := <-connection.confirmation
	if !confirmation.Ack {
		return fmt.Errorf("Confirmation was NOT acked for tag: %d", confirmation.DeliveryTag)
	}

	return nil
}
