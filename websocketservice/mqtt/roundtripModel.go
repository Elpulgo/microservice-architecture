package mqtt

import (
	"encoding/json"
	"fmt"
	"os"
)

// RoundTrip handles the values sent from client for a roundtrip
type RoundTrip struct {
	Key   string
	Value string
}

// ConvertToByteArray converts the model to a byte array
func (model *RoundTrip) ConvertToByteArray() []byte {
	var byteArray, err = json.Marshal(model)
	if err != nil {
		fmt.Println("Failed to convert model to byte array!")
		os.Exit(1)
	}

	return byteArray
}

func ConvertFromByteArray(bytes []byte) RoundTrip {
	var roundtrip *RoundTrip
	var err = json.Unmarshal(bytes, &roundtrip)
	if err != nil {
		fmt.Printf("Failed to convert from byte array to model %s", err.Error())
	}

	return RoundTrip{
		Key:   "tjena",
		Value: "mmmmm",
	}
}
