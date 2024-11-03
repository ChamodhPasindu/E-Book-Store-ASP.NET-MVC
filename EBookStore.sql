CREATE DATABASE EBookStoreDB;
GO

USE EBookStoreDB;
GO

CREATE TABLE Users (
    UserID INT IDENTITY(1,1) PRIMARY KEY,  
    FirstName NVARCHAR(100) NOT NULL,      
    LastName NVARCHAR(100) NOT NULL,       
    Email NVARCHAR(255) NOT NULL UNIQUE, 
    PasswordHash NVARCHAR(255) NOT NULL, 
    PhoneNumber NVARCHAR(15),              
    Address NVARCHAR(255),              
    Role NVARCHAR(50) CHECK (Role IN ('Customer', 'Admin')) NOT NULL, 
    RegistrationDate DATETIME DEFAULT GETDATE(),
    IsActive BIT DEFAULT 1,               
    LastLoginDate DATETIME                
);

CREATE TABLE Books (
    BookID INT IDENTITY(1,1) PRIMARY KEY, 
    Title NVARCHAR(255) NOT NULL,      
    Author NVARCHAR(255) NOT NULL,        
    Category NVARCHAR(100),              
    Price DECIMAL(10, 2) NOT NULL,     
    QuantityInStock INT NOT NULL,      
    Description NVARCHAR(MAX),            
    PublicationDate DATETIME,            
    ISBN NVARCHAR(50) UNIQUE NOT NULL,   
    IsActive BIT DEFAULT 1,               
    CreatedDate DATETIME DEFAULT GETDATE(),
    UpdatedDate DATETIME DEFAULT GETDATE() 
);

CREATE TABLE Orders (
    OrderID INT IDENTITY(1,1) PRIMARY KEY, 
    UserID INT NOT NULL,                 
    OrderDate DATETIME DEFAULT GETDATE(), 
    TotalAmount DECIMAL(10, 2) NOT NULL,   
    OrderStatus NVARCHAR(50) CHECK (OrderStatus IN ('Pending', 'Shipped', 'Delivered', 'Canceled')) NOT NULL,
    ShippingAddress NVARCHAR(255) NOT NULL,
    FOREIGN KEY (UserID) REFERENCES Users(UserID)
);

CREATE TABLE OrderDetails (
    OrderDetailID INT IDENTITY(1,1) PRIMARY KEY, 
    OrderID INT NOT NULL,                        
    BookID INT NOT NULL,                   
    Quantity INT NOT NULL,                      
    Price DECIMAL(10, 2) NOT NULL,             
    FOREIGN KEY (OrderID) REFERENCES Orders(OrderID), 
    FOREIGN KEY (BookID) REFERENCES Books(BookID)     
);

CREATE TABLE Feedback (
    FeedbackID INT IDENTITY(1,1) PRIMARY KEY, 
    UserID INT NOT NULL,                     
    BookID INT NOT NULL,                     
    FeedbackText NVARCHAR(MAX),             
    Rating INT CHECK (Rating BETWEEN 1 AND 5),
    FeedbackDate DATETIME DEFAULT GETDATE(), 
    FOREIGN KEY (UserID) REFERENCES Users(UserID),
    FOREIGN KEY (BookID) REFERENCES Books(BookID) 
);

