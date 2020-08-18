FROM golang:1.15.0-alpine3.12 AS build

RUN apk add --update git

WORKDIR /go/src/webservice
COPY . .

RUN go get -d -v \
  && go install -v \
  && go build -o webservice

##

FROM alpine:latest
COPY --from=build /go/bin/webservice /go/bin/
EXPOSE 8080
CMD ["/go/bin/webservice"]