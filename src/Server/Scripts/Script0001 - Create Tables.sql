CREATE TABLE cpfs (
    cpf bytea PRIMARY KEY,
    score int NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

