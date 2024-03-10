CREATE TABLE [ticker](
  [ticker_id] INTEGER PRIMARY KEY AUTOINCREMENT, 
  [name] NVARCHAR UNIQUE);

CREATE TABLE [alert](
  [alert_id] INT PRIMARY KEY ON CONFLICT IGNORE NOT NULL, 
  [ticker_id] INT NOT NULL REFERENCES [ticker]([ticker_id]), 
  [timeframe] TEXT NOT NULL, 
  [created_at] DATETIME NOT NULL, 
  [message] TEXT, 
  [strategy] TEXT, 
  [price] DECIMAL, 
  [order_type] TEXT NOT NULL, 
  [order_action] TEXT NOT NULL);

CREATE TABLE [daily_price](
  [ticker_id] INTEGER REFERENCES [ticker]([ticker_id]), 
  [date] datetime NOT NULL, 
  [close] decimal, 
  [high] decimal, 
  [low] decimal, 
  [open] decimal, 
  [volume] bigint, 
  PRIMARY KEY([ticker_id], [date]) ON CONFLICT IGNORE, 
  UNIQUE([ticker_id], [date]) ON CONFLICT IGNORE);

CREATE TABLE [fifteen_minute_price](
  [ticker_id] INTEGER REFERENCES [ticker]([ticker_id]), 
  [date] datetime NOT NULL, 
  [close] decimal, 
  [high] decimal, 
  [low] decimal, 
  [open] decimal, 
  [volume] bigint, 
  PRIMARY KEY([ticker_id], [date]) ON CONFLICT IGNORE);

CREATE TABLE [one_hour_price](
  [ticker_id] INTEGER REFERENCES [ticker]([ticker_id]), 
  [date] datetime NOT NULL, 
  [close] decimal, 
  [high] decimal, 
  [low] decimal, 
  [open] decimal, 
  [volume] bigint, 
  PRIMARY KEY([ticker_id], [date]) ON CONFLICT IGNORE);

CREATE TABLE [option_contract](
  [option_contract_id] INTEGER PRIMARY KEY AUTOINCREMENT, 
  [ticker_id] INTEGER NOT NULL, 
  [expired_on] DATETIME NOT NULL, 
  [strike] DECIMAL NOT NULL, 
  [option_right] CHAR NOT NULL, 
  CONSTRAINT [fk_ticker] FOREIGN KEY([ticker_id]) REFERENCES [ticker]([ticker_id]));

CREATE TABLE [option_contract_high_low_target](
  [option_contract_id] INTEGER, 
  [level_high] DECIMAL NOT NULL, 
  [level_low] DECIMAL NOT NULL, 
  [center] DECIMAL NOT NULL);

CREATE TABLE [swing_point_option_position](
  [swing_point_order_id] INTEGER PRIMARY KEY AUTOINCREMENT, 
  [option_contract_id] INTEGER, 
  [quantity] INTEGER NOT NULL, 
  [avg_price] DECIMAL NOT NULL, 
  [account_id] TEXT NOT NULL, 
  [is_closed] INT NOT NULL);

CREATE TABLE [thirty_minute_price](
  [ticker_id] INTEGER REFERENCES [ticker]([ticker_id]), 
  [date] datetime NOT NULL, 
  [close] decimal, 
  [high] decimal, 
  [low] decimal, 
  [open] decimal, 
  [volume] bigint, 
  PRIMARY KEY([ticker_id], [date]) ON CONFLICT IGNORE, 
  UNIQUE([ticker_id], [date]) ON CONFLICT IGNORE);

CREATE UNIQUE INDEX [idx_daily_ticker_id_date]
ON [daily_price](
  [ticker_id], 
  [date]);

CREATE UNIQUE INDEX [idx_fifteen_minute_ticker_id_date]
ON [fifteen_minute_price](
  [ticker_id], 
  [date]);

CREATE UNIQUE INDEX [idx_one_hour_ticker_id_date]
ON [one_hour_price](
  [ticker_id], 
  [date]);

CREATE UNIQUE INDEX [idx_thirty_minute_price_ticker_id_date]
ON [daily_price](
  [ticker_id], 
  [date]);

