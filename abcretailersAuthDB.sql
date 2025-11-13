CREATE TABLE Users(
	Id				INT PRIMARY KEY IDENTITY(1,1),
	Username		NVARCHAR(100) NOT NULL,
	PasswordHash	NVARCHAR(256) NOT NULL,
	Role			NVARCHAR(20) --'Customer' or 'Admin'
);

INSERT INTO Users (Username, PasswordHash, Role)
VALUES
('customer1', 'password123', 'Customer'),
('admin1', 'adminpass', 'Admin');

SELECT * FROM Users;

CREATE TABLE Cart (
	Id					INT PRIMARY KEY IDENTITY,
	CustomerUsername	NVARCHAR(100),
	ProductID			NVARCHAR(100),
	Quantity			INT
);

EXEC sp_rename 'Cart.ProductId', 'ProductID', 'COLUMN';

SELECT * FROM Cart;
