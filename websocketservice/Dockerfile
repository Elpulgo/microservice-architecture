FROM golang:1.15.0-alpine3.12 AS build

RUN apk add --update git

WORKDIR /go/src/websocketservice
COPY . .

RUN go get -d -v \
  && go install -v \
  && go build -o websocketservice

##

FROM alpine:latest
COPY --from=build /go/bin/websocketservice /go/bin/
EXPOSE 8010
CMD ["/go/bin/websocketservice"]