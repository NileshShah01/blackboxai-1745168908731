-- Cyber Cafe Management System Database Schema
PRAGMA foreign_keys = ON;

CREATE TABLE Users (
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  Username TEXT UNIQUE NOT NULL,
  PasswordHash TEXT NOT NULL,
  CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE Sessions (
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  UserId INTEGER NOT NULL,
  StartTime DATETIME NOT NULL,
  EndTime DATETIME,
  TotalTime INTEGER, -- in seconds
  AmountPaid REAL,
  FOREIGN KEY(UserId) REFERENCES Users(Id)
);

CREATE TABLE Commands (
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  ClientId TEXT NOT NULL,
  CommandType TEXT NOT NULL CHECK(CommandType IN ('LOCK', 'UNLOCK', 'RESTART', 'LOGOUT')),
  SentAt DATETIME DEFAULT CURRENT_TIMESTAMP,
  Executed BOOLEAN DEFAULT 0
);

-- Default admin user (password: admin123)
INSERT INTO Users (Username, PasswordHash) 
VALUES ('admin', '8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918');