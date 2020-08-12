// Copyright 2013 The Gorilla WebSocket Authors. All rights reserved.
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file.

package main

import (
	"encoding/json"
	"flag"
	"log"
	"net/http"
)

var addr = flag.String("addr", "127.0.0.1:8080", "http service address")

func main() {
	flag.Parse()
	hub := newHub()
	go hub.run()

	http.HandleFunc("/ws", func(w http.ResponseWriter, r *http.Request) {
		serveWs(hub, w, r)
	})

	http.HandleFunc("/api/hello", func(w http.ResponseWriter, r *http.Request) {
		enableCors(&w)
		var value postModel
		if err := json.NewDecoder(r.Body).Decode(&value); err != nil {
			log.Println("Failed to decode value from HTTP Request!")
		}

		log.Println("Got a request from HTTP!: ", value.Body)
	})

	log.Println("Web api started, listening on ws://127.0.0.1:8080")

	err := http.ListenAndServe(*addr, nil)
	if err != nil {
		log.Fatal("ListenAndServe: ", err)
	}
}

func enableCors(w *http.ResponseWriter) {
	(*w).Header().Add("Connection", "keep-alive")
	(*w).Header().Add("Access-Control-Allow-Methods", "POST, OPTIONS, GET, DELETE, PUT")
	(*w).Header().Add("Access-Control-Max-Age", "86400")
	(*w).Header().Set("Access-Control-Allow-Origin", "http://localhost:5000")
	(*w).Header().Set("Access-Control-Allow-Headers", "Content-Type")
}

type postModel struct {
	Body string
}
