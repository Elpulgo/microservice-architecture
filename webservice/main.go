// Copyright 2013 The Gorilla WebSocket Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.

package main

import (
	"github.com/streadway/amqp"
	"encoding/json"
	"flag"
	"fmt"
	"log"
	"net/http"
	"os"
	"webservice/mqtt"
)

var addr = flag.String("addr", ":8080", "http service address")
var channel *amqp.Channel

func main() {
	flag.Parse()

	channel := mqtt.Setup("events")
	
	http.HandleFunc("/api/hello", func(w http.ResponseWriter, r *http.Request) {
		enableCors(&w)
		var model postModel
		if err := json.NewDecoder(r.Body).Decode(&model); err != nil {
			log.Println("Failed to decode value from HTTP Request!")
		}
		
		log.Printf("Got a request from HTTP:, Key: '%s', Value: '%s', will publish to MQTT broker!", model.Key, model.Value)
		publishMqtt(channel, model)
	})

	log.Println("Webservice started, listening on http://localhost:8080")

	err := http.ListenAndServe(*addr, nil)
	if err != nil {
		log.Fatal("ListenAndServe: ", err)
		os.Exit(1)
	}
}

func enableCors(w *http.ResponseWriter) {
	(*w).Header().Add("Connection", "keep-alive")
	(*w).Header().Add("Access-Control-Allow-Methods", "POST, OPTIONS, GET, DELETE, PUT")
	(*w).Header().Add("Access-Control-Max-Age", "86400")
	(*w).Header().Set("Access-Control-Allow-Origin", "*")
	(*w).Header().Set("Access-Control-Allow-Headers", "Content-Type")
}

type postModel struct {
	Key   string
	Value string
}

func (model *postModel) convertToByteArray() []byte {

	var byteArray, err = json.Marshal(model)
	if err != nil {
		fmt.Println("Failed to convert model to byte array!")
		os.Exit(1)
	}

	return byteArray
}


func publishMqtt(channel *amqp.Channel, message postModel){
	mqttQueueModel := mqtt.QueueModel{
		Channel: channel,
		Exchange: "events",
		QueueName: "events",
	}

	mqtt.SetupQueue(mqttQueueModel)

	mqttPublishModel := mqtt.PublishModel{
		Channel: channel,
		Exchange: "events",
		Message: amqp.Publishing{
			Body: message.convertToByteArray(),
		},
	}

	mqtt.Publish(mqttPublishModel)
	fmt.Println("Successfully published message")
}