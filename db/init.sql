CREATE TABLE IF NOT EXISTS payments (
    payment_id UUID PRIMARY KEY,
    amount DECIMAL(10, 2) NOT NULL,
    status INT NOT NULL,
    correlation_id UUID NOT NULL,
    created_at TIMESTAMP NOT NULL,
    processed_at TIMESTAMP, 
    processor_name INT
);

CREATE INDEX IF NOT EXISTS idx_payments_correlation_id ON payments(correlation_id);
CREATE INDEX IF NOT EXISTS idx_payments_status ON payments(status);
CREATE INDEX IF NOT EXISTS idx_payments_processed_at ON payments(processed_at);