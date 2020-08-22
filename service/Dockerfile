# Dockerfile for creating a statically-linked Rust application using docker's
# multi-stage build feature. This also leverages the docker build cache to avoid
# re-downloading dependencies if they have not changed.
FROM rust:latest AS build
WORKDIR /usr/src

# Download the target for static linking.
RUN rustup target add x86_64-unknown-linux-musl

# We'll get to what this file is below!
##  COPY ./docker-dummy.rs ./
# If this changed likely the Cargo.toml changed so lets trigger the
# recopying of it anyways
## COPY Cargo.lock ./
## COPY Cargo.toml ./
# We'll get to what this substitution is for but replace main.rs with
# lib.rs if this is a library
## RUN sed -i 's/src\/main.rs/docker-dummy.rs/' Cargo.toml
# Drop release if you want debug builds. This step cache's our deps!
## RUN cargo build --release
# Now return the file back to normal
## RUN sed -i 's/docker-dummy.rs/src\/main.rs/' Cargo.toml
# Copy the rest of the files into the container


# Create a dummy project and build the app's dependencies.
# If the Cargo.toml or Cargo.lock files have not changed,
# we can use the docker build cache and skip these (typically slow) steps.
RUN USER=root cargo new service
WORKDIR /usr/src/service
COPY Cargo.toml Cargo.lock ./
#COPY src ./src
RUN cargo build --release

# Copy the source and build the application.
COPY src ./src
RUN cargo install --target x86_64-unknown-linux-musl --path .

# Copy the statically-linked binary into a scratch container.
FROM scratch
COPY --from=build /usr/local/cargo/bin/service .
USER 1000
CMD ["./service"]