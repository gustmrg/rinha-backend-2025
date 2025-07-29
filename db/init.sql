CREATE DATABASE rinha;

CREATE TABLE IF NOT EXISTS payments (
    payment_id UUID PRIMARY KEY,
    amount DECIMAL(10, 2) NOT NULL,
    status INT NOT NULL,
    correlation_id UUID NOT NULL,
    created_at TIMESTAMP NOT NULL,
    processed_at TIMESTAMP 
)