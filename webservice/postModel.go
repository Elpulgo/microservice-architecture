package main

import (
	"encoding/json"
	"fmt"
	"os"
)

// PostModel handles the values sent from client
type PostModel struct {
	Key   string
	Value string
}

// ConvertToByteArray converts the model to a byte array
func (model *PostModel) ConvertToByteArray() []byte {
	var byteArray, err = json.Marshal(model)
	if err != nil {
		fmt.Println("Failed to convert model to byte array!")
		os.Exit(1)
	}

	return byteArray
}
