// Copyright 2013 The Gorilla WebSocket Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.

package main

import (
	"flag"
	"log"
	"net/http"
	"os"
	"os/signal"
	"syscall"
	"websocketservice/hub"
	"websocketservice/mqtt"

	serviceregistration "github.com/elpulgo/microservice-architecture/shared/go-shared"
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
	})

	consumer.Consume(websocketHub)

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

	log.Println("Websocket service started, listening on ws://+:8010/ws")

	err := http.ListenAndServe(*addr, nil)
	if err != nil {
		log.Fatal("ListenAndServe: ", err)
		os.Exit(1)
	}
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
