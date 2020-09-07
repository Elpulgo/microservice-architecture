package serviceregistration

import (
	"log"
	"net"
	"net/url"
	"os"
	"strconv"

	consulapi "github.com/hashicorp/consul/api"
)

// RegisterServiceWithConsul registers this service to consul
func RegisterServiceWithConsul() *consulapi.Agent {
	config := consulapi.DefaultConfig()
	config.Address = getConsulAddress()
	consul, err := consulapi.NewClient(config)

	if err != nil {
		log.Fatalln(err)
	}

	registration := buildRegistration()
	agent := consul.Agent()
	err = agent.ServiceRegister(registration)
	if err != nil {
		log.Fatalln("Failed to register service: " + err.Error())
	}

	log.Println("Successfully registered service with Consul!")
	return agent
}

// UnregisterServiceWithConsul unregisters this service from Consul
func UnregisterServiceWithConsul(agent *consulapi.Agent) {
	serviceID := getServiceID()
	err := agent.ServiceDeregister(serviceID)
	if err != nil {
		log.Fatalln("Failed to unregister service with Consul!")
	}

	log.Println("Successfully unregistered service with Consul!")
}

func getConsulAddress() string {
	consulServiceAddress, exists := os.LookupEnv("CONSUL:DISCOVERYADDRESS")
	if !exists {
		log.Fatalln("Consul service discovery address is missing in environment variables, can't register service to Consul!")
	}
	return consulServiceAddress
}

func getServiceID() string {
	serviceID, exists := os.LookupEnv("CONSUL:SERVICEID")
	if !exists {
		log.Fatalln("ServiceId is missing in environment variables, can't register service to Consul!")
	}
	return serviceID
}

func buildRegistration() *consulapi.AgentServiceRegistration {

	serviceID := getServiceID()

	serviceName, exists := os.LookupEnv("CONSUL:SERVICENAME")
	if !exists {
		log.Fatalln("ServiceName is missing in environment variables, can't register service to Consul!")
	}

	serviceAddress, exists := os.LookupEnv("CONSUL:SERVICEADDRESS")
	if !exists {
		log.Fatalln("ServiceAddress is missing in environment variables, can't register service to Consul!")
	}

	url, err := url.Parse(serviceAddress)
	if err != nil {
		log.Fatalln("Failed to parse ServiceAddress, not a valid uri! Can't register service to Consul!")
	}

	host, port, err := net.SplitHostPort(url.Host)
	if err != nil {
		log.Fatalln("Failed to parse ServiceAddress and get port, not a valid uri! Can't register service to Consul!")
	}

	parsedPort, err := strconv.Atoi(port)
	if err != nil {
		log.Fatalln("Failed to parse ServiceAddress and extract port, not a valid uri! Can't register service to Consul!")
	}

	registration := new(consulapi.AgentServiceRegistration)
	registration.ID = serviceID
	registration.Name = serviceName
	registration.Address = host
	registration.Port = parsedPort
	return registration
}
