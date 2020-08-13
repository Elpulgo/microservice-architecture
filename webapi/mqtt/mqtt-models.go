package mqtt

import "github.com/streadway/amqp"

// PublishModel handles info about where the message should be published
type PublishModel struct {
	Channel  *amqp.Channel
	Exchange string
	Message  amqp.Publishing
}

// QueueModel handles info about how to setup and declare queues and bind to exchanges
type QueueModel struct {
	Exchange  string
	QueueName string
	Channel   *amqp.Channel
}
