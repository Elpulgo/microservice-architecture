package main

import (
	"encoding/json"
	"flag"
	"log"
	"net/http"
	"os"
	"os/signal"
	"syscall"
	"webservice/mqtt"

	models "github.com/elpulgo/microservice-architecture/shared/go-shared/models"
	serviceregistration "github.com/elpulgo/microservice-architecture/shared/go-shared/serviceregistration"
	"github.com/streadway/amqp"
)

var addr = flag.String("addr", ":8080", "http service address")
var exchangeName = flag.String("exchange", "exchange_events", "Name for MQTT exchange")
var roundTripQueueName = flag.String("roundTripQueue", "events", "Name of the MQTT queue to publish roundtrip messages to")
var connectionName = flag.String("connection", "event_producer", "Name of the MQTT connection")

func main() {
	flag.Parse()

	connection := initMQTTConnection()

	// Will publish key/value pair to MQTT broker for roundtrip in the microservice architecture, and come back
	// via websocket to client. This endpoint will wait for confirmation of message from server and respond with a status
	// if message was confirmed or not by consumer
	http.HandleFunc("/api/roundtrip", func(w http.ResponseWriter, r *http.Request) {
		var roundtrip models.RoundTrip
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

	consulAgent := serviceregistration.RegisterServiceWithConsul()

	idleChan := make(chan struct{})

	go func() {
		// Handle sigterm and await termChan signal
		termChan := make(chan os.Signal, 1)
		signal.Notify(termChan, os.Interrupt, syscall.SIGINT, syscall.SIGTERM)
		term := <-termChan // Blocks here until interrupted

		log.Println("Shutdown: ", term)
		serviceregistration.UnregisterServiceWithConsul(consulAgent)
		close(idleChan)
	}()

	log.Println("Webservice started, listening on http://+:8080")

	err := http.ListenAndServe(*addr, enableCors(http.DefaultServeMux))
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

func publish(connection *mqtt.Connection, message models.RoundTrip) bool {
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
