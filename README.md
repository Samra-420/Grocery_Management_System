# 🛒 Grocery Management System

## 📌 Project Overview
The **Grocery Management System** is a console-based application developed in **C# (.NET)** with **MongoDB integration**. It automates grocery store operations such as product management, billing, and user handling using **Object-Oriented Programming (OOP)** concepts.

The system is designed with **role-based access control** for Admin, Cashier, and Customer, making it efficient, scalable, and easy to use.

---

## 🎯 Key Features
- Product Management (Add, Update, Delete)
- Billing System with Cart
- User Authentication
- Role-Based Access (Admin, Cashier, Customer)
- MongoDB Database Integration
- Modular and Scalable Architecture

---

## 👥 User Roles

### 🔹 Admin
- Manage Products
- Update Stock
- View Sales Reports

### 🔹 Cashier
- Create Bills
- Add Items to Cart
- Generate Receipts

### 🔹 Customer
- Browse Products
- Search Items
- Purchase Products
- View Purchase History

---

## 🧠 OOP Concepts Implemented

### 1. Encapsulation
Data is stored inside classes and accessed using properties.

### 2. Abstraction
Complex logic (database, billing) is hidden inside methods.

### 3. Inheritance
Admin, Cashier, and Customer inherit from the base `User` class.

### 4. Polymorphism
Same function behaves differently for different roles (method overriding).

### 5. Composition
Classes like `Bill` contain `CartItem`, and `CartItem` contains `Product`.

---

## 🧾 System Screenshots

### 🔹 Admin Panel
<p align="center">
  <img src="screenshots/admin1.png" width="45%">
  <img src="screenshots/admin2.png" width="45%">
</p>

<p align="center">
  <img src="screenshots/admin3.png" width="45%">
  <img src="screenshots/admin4.png" width="45%">
</p>

---

### 🔹 Cashier Panel
<p align="center">
  <img src="screenshots/cashier1.png" width="45%">
  <img src="screenshots/cashier2.png" width="45%">
</p>

<p align="center">
  <img src="screenshots/cashier3.png" width="45%">
  <img src="screenshots/cashier4.png" width="45%">
</p>

---

### 🔹 Customer Panel
<p align="center">
  <img src="screenshots/customer1.png" width="45%">
  <img src="screenshots/customer2.png" width="45%">
</p>

<p align="center">
  <img src="screenshots/customer3.png" width="45%">
  <img src="screenshots/customer4.png" width="45%">
</p>

---

## 🗄️ Database (MongoDB)
MongoDB is used to store:
- Products
- Users
- Bills
- Transactions

---

## 💻 Example Code

### MongoDB Connection
```csharp
var client = new MongoClient("mongodb://localhost:27017");
var database = client.GetDatabase("GroceryDB");
