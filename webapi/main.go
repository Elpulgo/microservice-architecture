package main

import (
	"fmt"
	"net/http"
)

func init() {

}

func main() {

	error := http.ListenAndServe("3001", nil)
	if error != nil {
		fmt.Printf("Hello")
	}
}
