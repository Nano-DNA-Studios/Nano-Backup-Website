CREATE TABLE nanobackupdatabase(
	id SERIAL PRIMARY KEY,
	name TEXT NOT NULL,
	is_file BOOLEAN NOT NULL,
	size_bytes BIGINT DEFAULT 0,
	path TEXT,
	parent_id INTEGER REFERENCES nanobackupdatabase(id),
	file_data BYTEA
);