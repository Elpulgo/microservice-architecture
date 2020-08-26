// Copyright 2013 The Gorilla WebSocket Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.

package main

import (
	"encoding/json"
	"flag"
	"log"
	"net/http"
	"os"
	"websocketservice/hub"
	"websocketservice/mqtt"
)

var addr = flag.String("addr", ":8010", "http websocket service address")
var connectionName = flag.String("connection", "event_consumer", "Name of the MQTT connection")

func main() {
	flag.Parse()
	websocketHub := hub.NewHub()
	go websocketHub.Run()

	consumer := initMQTTConnection()

	http.HandleFunc("/ws", func(w http.ResponseWriter, r *http.Request) {
		hub.ServeWs(websocketHub, w, r)
		testModel := postModel{
			Key:   "hello",
			Value: "yeeesh",
		}

		websocketHub.Broadcast <- testModel.convertToByteArray()
	})

	consumer.Consume(websocketHub)

	log.Println("Websocket service started, listening on ws://localhost:8010/ws")

	err := http.ListenAndServe(*addr, nil)
	if err != nil {
		log.Fatal("ListenAndServe: ", err)
		os.Exit(1)
	}
}

type postModel struct {
	Key   string
	Value string
}

func (model *postModel) convertToByteArray() []byte {

	var byteArray, err = json.Marshal(model)
	if err != nil {
		log.Fatal("Failed to convert model to byte array!")
		os.Exit(1)
	}

	return byteArray
}

func initMQTTConnection() *mqtt.Consumer {
	consumer := mqtt.NewConsumer(
		*connectionName, // name
	)

	if err := consumer.Connect(); err != nil {
		panic(err)
	}

	if err := consumer.DeclareQueue(); err != nil {
		panic(err)
	}

	return consumer
}
