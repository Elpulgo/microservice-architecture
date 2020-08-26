package main

import (
	"encoding/json"
	"fmt"
	"os"
)

// BatchModel handles batchvalues
type BatchModel struct {
	HashKey string
	Key     string
	Value   string
}

// ConvertToByteArray converts the model to a byte array
func (model *BatchModel) ConvertToByteArray() []byte {
	var byteArray, err = json.Marshal(model)
	if err != nil {
		fmt.Println("Failed to convert batch model to byte array!")
		os.Exit(1)
	}

	return byteArray
}
