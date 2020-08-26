package main

import (
	"encoding/json"
	"flag"
	"log"
	"net/http"
	"os"
	"strconv"
	"webservice/mqtt"

	"github.com/google/uuid"
	"github.com/streadway/amqp"
)

var addr = flag.String("addr", ":8080", "http service address")
var exchangeName = flag.String("exchange", "exchange_events", "Name for MQTT exchange")
var roundTripQueueName = flag.String("roundTripQueue", "events", "Name of the MQTT queue to publish roundtrip messages to")
var batchQueueName = flag.String("batchQueue", "batch", "Name of the MQTT queue to publish roundtrip messages to")
var connectionName = flag.String("connection", "event_producer", "Name of the MQTT connection")
var batchSize = 100

func main() {
	flag.Parse()
	size, err := strconv.Atoi(os.Getenv("BATCH_SIZE"))
	if err != nil {
		batchSize = 100
		log.Println("Failed to set BATCH_SIZE from env variable, will default to 100")
	} else {
		batchSize = size
	}

	connection := initMQTTConnection()

	// Will publish key/value pair to MQTT broker for roundtrip in the microservice architecture, and come back
	// via websocket to client. This endpoint will wait for confirmation of message from server and respond with a status
	// if message was confirmed or not by consumer
	http.HandleFunc("/api/roundtrip", func(w http.ResponseWriter, r *http.Request) {
		var roundtrip RoundTrip
		if err := json.NewDecoder(r.Body).Decode(&roundtrip); err != nil {
			log.Println("Failed to decode value from HTTP Request!")
		}

		log.Printf("Got a request to publish:, Key: '%s', Value: '%s', will publish to MQTT broker!", roundtrip.Key, roundtrip.Value)
		if succeded := publish(connection, roundtrip); succeded == true {
			w.WriteHeader(http.StatusOK)
		} else {
			w.WriteHeader(http.StatusInternalServerError)
		}
	})

	// Will publish a batch of key/value pairs to MQTT broker, won't do a roundtrip, but will
	// wait for confirmation of all messages and will be saved in Redis.
	http.HandleFunc("/api/batch", func(w http.ResponseWriter, r *http.Request) {
		log.Println("Got a request to publish batch. Size of batch: ", batchSize)
		if succeded := publishBatch(connection); succeded == true {
			w.WriteHeader(http.StatusOK)
		} else {
			w.WriteHeader(http.StatusInternalServerError)
		}
	})

	log.Println("Webservice started, listening on http://localhost:8080")

	err = http.ListenAndServe(*addr, enableCors(http.DefaultServeMux))
	if err != nil {
		log.Fatal("ListenAndServe: ", err)
		os.Exit(1)
	}
}

func enableCors(h http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		w.Header().Add("Connection", "keep-alive")
		w.Header().Add("Access-Control-Allow-Methods", "POST, OPTIONS, GET, DELETE, PUT")
		w.Header().Add("Access-Control-Max-Age", "86400")
		w.Header().Set("Access-Control-Allow-Origin", "*")
		w.Header().Set("Access-Control-Allow-Headers", "Content-Type")
		h.ServeHTTP(w, r)
	})
}

func initMQTTConnection() *mqtt.Connection {
	connection := mqtt.NewConnection(
		*connectionName, // name
		*exchangeName,   // exchange name
		[]string{
			*roundTripQueueName, // queues
			*batchQueueName,
		})

	if err := connection.Connect(); err != nil {
		panic(err)
	}

	if err := connection.DeclareChannel(); err != nil {
		panic(err)
	}

	if err := connection.BindQueues(); err != nil {
		panic(err)
	}

	return connection
}

func publish(connection *mqtt.Connection, message RoundTrip) bool {
	mqttMessage := amqp.Publishing{
		ContentType:  "application/json",
		DeliveryMode: amqp.Transient,
		Body:         message.ConvertToByteArray(),
	}

	if err := connection.Publish(mqttMessage, *roundTripQueueName); err != nil {
		log.Fatalf("Failed to publish messaged with key: %s and value: %s, err: %s!", message.Key, message.Value, err.Error())
		return false
	}

	log.Printf("Successfully published message with key: %s and value: %s!", message.Key, message.Value)
	return true
}

func publishBatch(connection *mqtt.Connection) bool {
	mqttMessages := []amqp.Publishing{}
	i := 0
	batchKey := uuid.New().String()
	for i < batchSize {
		message := BatchModel{
			HashKey: batchKey,
			Key:     batchKey + "_key_" + strconv.Itoa(i),
			Value:   batchKey + "_value_" + strconv.Itoa(i),
		}

		mqttMessage := amqp.Publishing{
			ContentType:  "application/json",
			DeliveryMode: amqp.Transient,
			Body:         message.ConvertToByteArray(),
		}

		mqttMessages = append(mqttMessages, mqttMessage)
		i++
	}

	if err := connection.PublishBatch(mqttMessages, *batchQueueName); err != nil {
		log.Fatalf("Failed to publish batch, err: %s!", err.Error())
		return false
	}

	log.Printf("Successfully published batch of (%d) messages!", batchSize)
	return true
}
