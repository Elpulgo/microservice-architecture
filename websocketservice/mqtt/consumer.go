package mqtt

import (
	"fmt"
	"log"
	"websocketservice/hub"

	"github.com/streadway/amqp"
)

// Consume listens on incoming messages on the queue name
func (consumer *Consumer) Consume(hub *hub.Hub) error {
	deliveries, err := consumer.channel.Consume(
		consumer.queue.Name, // queue name
		"consumer-tag",      // consumer tag (should not be blank)
		true,                // auto ack
		false,               // exclusive
		false,               // noLocal
		false,               // noWait
		nil,                 // args
	)
	if err != nil {
		log.Printf("Queue consume error: %s", err.Error())
		return fmt.Errorf("Queue Consume: %s", err)
	}

	go handleDeliveries(deliveries, hub, consumer.err)

	return nil
}

func handleDeliveries(deliveries <-chan amqp.Delivery, hub *hub.Hub, done chan error) {
	for delivery := range deliveries {
		log.Printf(
			"Delivery received, [%v] %q, will broadcast to client via websocket!",
			delivery.DeliveryTag,
			delivery.Body,
		)

		hub.Broadcast <- delivery.Body
	}
	log.Printf("Handle deliveries: deliveries channel closed")
	done <- nil
}
