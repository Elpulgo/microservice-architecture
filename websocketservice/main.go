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
)

var addr = flag.String("addr", ":8010", "http websocket service address")

func main() {
	flag.Parse()
	hub := newHub()
	go hub.run()	

	http.HandleFunc("/ws", func(w http.ResponseWriter, r *http.Request) {
		serveWs(hub, w, r)
		testModel := postModel{
			Key: "hello",
			Value: "yeeesh",
		}

		hub.broadcast <- testModel.convertToByteArray() 
	})

	log.Println("Websocket service started, listening on ws://localhost:8010/ws")

	err := http.ListenAndServe(*addr, nil)
	if err != nil {
		log.Fatal("ListenAndServe: ", err)
		os.Exit(1)
	}
}

// func enableCors(w *http.ResponseWriter) {
// 	(*w).Header().Add("Connection", "keep-alive")
// 	(*w).Header().Add("Access-Control-Allow-Methods", "POST, OPTIONS, GET, DELETE, PUT")
// 	(*w).Header().Add("Access-Control-Max-Age", "86400")
// 	(*w).Header().Set("Access-Control-Allow-Origin", "*")
// 	(*w).Header().Set("Access-Control-Allow-Headers", "Content-Type")
// }

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