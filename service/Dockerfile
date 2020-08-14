FROM rust:latest

WORKDIR /usr/src/service
COPY . .

RUN cargo install --path .

CMD ["service"]