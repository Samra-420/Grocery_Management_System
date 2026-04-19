using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FreshMartGrocery
{
    // ===== DATA MODELS =====

    // ENCAPSULATION: Data and properties bundled inside a class
    class Product
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }       // get/set = encapsulation
        public int ProductId { get; set; }
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public decimal Price { get; set; }
        public int Stock { get; set; }
    }

    // COMPOSITION: CartItem HAS-A Product (not inherited, but contained)
    class CartItem
    {
        public Product Product { get; set; } = new Product();   // HAS-A relationship
        public int Quantity { get; set; }

        // ABSTRACTION: Hides the calculation logic behind a simple property
        public decimal Total => Product.Price * Quantity;
    }

    // ENCAPSULATION + COMPOSITION: Bill contains a list of CartItems
    class Bill
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        public int BillNo { get; set; }
        public DateTime Date { get; set; }
        public string CustomerName { get; set; } = "";
        public List<CartItem> Items { get; set; } = new List<CartItem>();  // COMPOSITION: Bill HAS-A list of CartItems
        public decimal Subtotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
        public decimal Paid { get; set; }
        public decimal Change { get; set; }
    }

    // ENCAPSULATION: User data is bundled with Role-based access control
    class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";

        // POLYMORPHISM (via Role): Same User class behaves as Admin, Cashier, or Customer
        public string Role { get; set; } = "";
        public DateTime RegDate { get; set; }
    }

    // ===== MONGODB CONTEXT =====

    // ABSTRACTION: Hides all MongoDB connection details from the rest of the program
    class MongoDbContext
    {
        private readonly IMongoDatabase _database;  // ENCAPSULATION: private field, not accessible outside

        public MongoDbContext()
        {
            try
            {
                var client = new MongoClient("mongodb://localhost:27017");
                _database = client.GetDatabase("FreshMartDB");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  [OK] Connected to MongoDB successfully!");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("  [ERROR] MongoDB Connection Failed: " + ex.Message);
                Console.ResetColor();
                throw;
            }
        }

        // ABSTRACTION: Expose collections as simple properties, hiding how they are fetched
        public IMongoCollection<Product> Products => _database.GetCollection<Product>("Products");
        public IMongoCollection<Bill> Bills => _database.GetCollection<Bill>("Bills");
        public IMongoCollection<User> Users => _database.GetCollection<User>("Users");
    }

    // ===== TABLE DRAWING HELPER =====

    // STATIC CLASS: No instances created — used directly as T.HRule(), T.Row() etc.
    // ABSTRACTION: Hides all table-drawing logic in one place
    static class T
    {
        // Box-drawing characters as const char (required to avoid CS0150 errors)
        public const char H = '\u2550';
        public const char V = '\u2551';
        public const char TL = '\u2554';
        public const char TR = '\u2557';
        public const char BL = '\u255A';
        public const char BR = '\u255D';
        public const char MT = '\u2566';
        public const char MB = '\u2569';
        public const char ML = '\u2560';
        public const char MR = '\u2563';
        public const char MC = '\u256C';

        // ABSTRACTION: Caller just says "fit this value in width w" — no formatting logic outside
        public static string Fit(string? val, int w, bool right = false)
        {
            val = val ?? "";
            if (val.Length > w) val = val.Substring(0, w - 1) + ".";
            return right ? val.PadLeft(w) : val.PadRight(w);
        }

        // ABSTRACTION: Draws top (0), middle (1), or bottom (2) border line
        public static void HRule(int[] cols, int type)
        {
            char left, cross, right;
            if (type == 0) { left = TL; cross = MT; right = TR; }
            else if (type == 2) { left = BL; cross = MB; right = BR; }
            else { left = ML; cross = MC; right = MR; }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("  ");
            Console.Write(left);
            for (int i = 0; i < cols.Length; i++)
            {
                for (int j = 0; j < cols[i] + 2; j++) Console.Write(H);
                if (i < cols.Length - 1) Console.Write(cross);
            }
            Console.WriteLine(right);
            Console.ResetColor();
        }

        // POLYMORPHISM: Same Row() method handles any number of columns with any alignment
        public static void Row(string[] cells, int[] cols, bool[] rightAlign)
        {
            Console.Write("  ");
            Console.Write(V);
            for (int i = 0; i < cells.Length; i++)
            {
                Console.Write(" ");
                Console.Write(Fit(cells[i], cols[i], rightAlign[i]));
                Console.Write(" ");
                Console.Write(V);
            }
            Console.WriteLine();
        }
    }

    // ===== MAIN PROGRAM =====

    class Program
    {
        static MongoDbContext db = new MongoDbContext();
        static List<Product> products = new List<Product>();
        static List<Bill> bills = new List<Bill>();
        static List<User> users = new List<User>();
        static List<CartItem> cart = new List<CartItem>();

        // POLYMORPHISM: loggedInUser can be Admin, Cashier, or Customer — same variable, different behavior
        static User? loggedInUser = null;
        static int nextBillNo = 1;
        static int nextUserId = 1;

        static readonly int[] ProdCols = { 4, 22, 14, 11, 6 };
        static readonly int[] BillCols = { 7, 15, 20, 12 };
        static readonly int[] CartCols = { 36, 5, 11, 11 };

        // ===== ENTRY POINT =====

        static void Main()
        {
            LoadData();
            InitializeDefaults();

            while (true)
            {
                if (loggedInUser == null)
                    ShowMainMenu();
                // POLYMORPHISM: Same loggedInUser variable routes to different panels based on Role
                else if (loggedInUser.Role == "Admin")
                    ShowAdminPanel();
                else if (loggedInUser.Role == "Cashier")
                    ShowCashierPanel();
                else if (loggedInUser.Role == "Customer")
                    ShowCustomerPanel();
            }
        }

        // ===== DATA =====

        static void LoadData()
        {
            try
            {
                products = db.Products.Find(_ => true).ToList();
                bills = db.Bills.Find(_ => true).ToList();
                users = db.Users.Find(_ => true).ToList();
                if (bills.Any()) nextBillNo = bills.Max(b => b.BillNo) + 1;
                if (users.Any()) nextUserId = users.Max(u => u.UserId) + 1;
            }
            catch (Exception ex) { ShowError("Load error: " + ex.Message); }
        }

        static void InitializeDefaults()
        {
            if (!products.Any())
            {
                var dp = new Product[]
                {
                    new Product { ProductId=1, Name="Rice (5kg)",        Category="Grains",    Price=350, Stock=50 },
                    new Product { ProductId=2, Name="Wheat Flour (5kg)", Category="Grains",    Price=280, Stock=40 },
                    new Product { ProductId=3, Name="Sugar (1kg)",       Category="Groceries", Price=55,  Stock=80 },
                    new Product { ProductId=4, Name="Tea (500g)",        Category="Beverages", Price=250, Stock=30 },
                    new Product { ProductId=5, Name="Milk (1L)",         Category="Dairy",     Price=120, Stock=25 },
                    new Product { ProductId=6, Name="Bread",             Category="Bakery",    Price=50,  Stock=20 },
                    new Product { ProductId=7, Name="Eggs (12pcs)",      Category="Dairy",     Price=100, Stock=40 },
                    new Product { ProductId=8, Name="Oil (1L)",          Category="Groceries", Price=220, Stock=35 },
                };
                foreach (var p in dp) db.Products.InsertOne(p);
                products.AddRange(dp);
            }
            if (!users.Any())
            {
                var du = new User[]
                {
                    new User { UserId=nextUserId++, Name="Store Owner",  Username="admin",   Password="admin123", Role="Admin",   RegDate=DateTime.Now },
                    new User { UserId=nextUserId++, Name="Rahul Sharma", Username="cashier", Password="cash123",  Role="Cashier", RegDate=DateTime.Now },
                };
                foreach (var u in du) db.Users.InsertOne(u);
                users.AddRange(du);
            }
        }

        // ===== CRUD =====

        // ABSTRACTION: These methods hide MongoDB operations behind simple method calls
        static void DbAddProduct(Product p) { db.Products.InsertOne(p); products.Add(p); }
        static void DbAddBill(Bill b) { db.Bills.InsertOne(b); bills.Add(b); }
        static void DbAddUser(User u) { db.Users.InsertOne(u); users.Add(u); }

        static void DbUpdateProduct(Product p)
        {
            db.Products.ReplaceOne(Builders<Product>.Filter.Eq(x => x.ProductId, p.ProductId), p);
            int idx = products.FindIndex(x => x.ProductId == p.ProductId);
            if (idx >= 0) products[idx] = p;
        }

        static void DbDeleteProduct(int id)
        {
            db.Products.DeleteOne(Builders<Product>.Filter.Eq(x => x.ProductId, id));
            products.RemoveAll(x => x.ProductId == id);
        }

        // ===== UI HELPERS =====

        static void ShowLogo()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine();
            Console.WriteLine("      ███████╗██████╗ ███████╗███████╗██╗  ██╗    ███╗   ███╗ █████╗ ██████╗ ████████╗");
            Console.WriteLine("      ██╔════╝██╔══██╗██╔════╝██╔════╝██║  ██║    ████╗ ████║██╔══██╗██╔══██╗╚══██╔══╝");
            Console.WriteLine("      █████╗  ██████╔╝█████╗  ███████╗███████║    ██╔████╔██║███████║██████╔╝   ██║   ");
            Console.WriteLine("      ██╔══╝  ██╔══██╗██╔══╝  ╚════██║██╔══██║    ██║╚██╔╝██║██╔══██║██╔══██╗   ██║   ");
            Console.WriteLine("      ██║     ██║  ██║███████╗███████║██║  ██║    ██║ ╚═╝ ██║██║  ██║██║  ██║   ██║   ");
            Console.WriteLine("      ╚═╝     ╚═╝  ╚═╝╚══════╝╚══════╝╚═╝  ╚═╝    ╚═╝     ╚═╝╚═╝  ╚═╝╚═╝  ╚═╝   ╚═╝  ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine();
            Console.WriteLine("                         WELCOME TO YOUR NEIGHBORHOOD STORE");
            Console.ResetColor();
            Console.WriteLine();
        }

        static void DrawHeader(string title)
        {
            ShowLogo();
            Console.ForegroundColor = ConsoleColor.Cyan;
            string line = "  " + new string(T.H, 70);
            Console.WriteLine(line);
            Console.Write("  ");
            Console.Write(T.V);
            Console.Write("  " + title.PadRight(67));
            Console.WriteLine(T.V);
            Console.WriteLine(line);
            Console.ResetColor();
        }

        // ABSTRACTION: Simple method names hide console color/formatting details
        static void ShowSuccess(string msg) { Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine("\n  [OK]  " + msg); Console.ResetColor(); }
        static void ShowError(string msg) { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine("\n  [ERR] " + msg); Console.ResetColor(); }
        static void ShowInfo(string msg) { Console.ForegroundColor = ConsoleColor.Yellow; Console.WriteLine("\n  [i]   " + msg); Console.ResetColor(); }

        static string GetInput(string prompt)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("\n  " + prompt + ": ");
            Console.ResetColor();
            return Console.ReadLine() ?? "";
        }

        static int GetInt(string prompt)
        {
            Console.Write("\n  " + prompt + ": ");
            int.TryParse(Console.ReadLine(), out int v);
            return v;
        }

        static decimal GetDecimal(string prompt)
        {
            Console.Write("\n  " + prompt + ": Rs.");
            decimal.TryParse(Console.ReadLine(), out decimal v);
            return v;
        }

        static void Pause()
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("\n  Press any key to continue...");
            Console.ReadKey();
            Console.ResetColor();
        }

        static string ReadPassword()
        {
            string pw = "";
            ConsoleKeyInfo k;
            do
            {
                k = Console.ReadKey(true);
                if (k.Key != ConsoleKey.Backspace && k.Key != ConsoleKey.Enter)
                {
                    pw += k.KeyChar;
                    Console.Write("*");
                }
                else if (k.Key == ConsoleKey.Backspace && pw.Length > 0)
                {
                    pw = pw.Substring(0, pw.Length - 1);
                    Console.Write("\b \b");
                }
            } while (k.Key != ConsoleKey.Enter);
            Console.WriteLine();
            return pw;
        }

        static int Menu(string title, string[] options)
        {
            int sel = 0;
            ConsoleKey key;
            do
            {
                DrawHeader(title);
                for (int i = 0; i < options.Length; i++)
                {
                    if (i == sel)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("\n  >> " + options[i]);
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.WriteLine("\n     " + options[i]);
                        Console.ResetColor();
                    }
                }
                key = Console.ReadKey(true).Key;
                if (key == ConsoleKey.UpArrow && sel > 0) sel--;
                if (key == ConsoleKey.DownArrow && sel < options.Length - 1) sel++;
            } while (key != ConsoleKey.Enter);
            return sel;
        }

        // ===== PRODUCT TABLE =====

        static void PrintProductTable(IEnumerable<Product> list)
        {
            int[] w = ProdCols;
            bool[] ra = { true, false, false, true, true };

            T.HRule(w, 0);
            T.Row(new[] { "ID", "Name", "Category", "Price(Rs)", "Stock" }, w, ra);
            T.HRule(w, 1);
            foreach (var p in list)
                T.Row(new[] { p.ProductId.ToString(), p.Name, p.Category, p.Price.ToString("F2"), p.Stock.ToString() }, w, ra);
            T.HRule(w, 2);
        }

        static void ViewProducts()
        {
            DrawHeader("PRODUCT LIST");
            PrintProductTable(products);
        }

        // ===== BILL TABLE =====

        static void PrintBillTable(IEnumerable<Bill> list, bool showTotal = false)
        {
            int[] w = BillCols;
            bool[] ra = { true, false, false, true };

            T.HRule(w, 0);
            T.Row(new[] { "Bill #", "Date", "Customer", "Amount(Rs)" }, w, ra);
            T.HRule(w, 1);

            decimal revenue = 0;
            foreach (var b in list)
            {
                T.Row(new[]
                {
                    b.BillNo.ToString(),
                    b.Date.ToString("dd/MM/yy HH:mm"),
                    b.CustomerName,
                    b.Total.ToString("F2")
                }, w, ra);
                revenue += b.Total;
            }

            if (showTotal)
            {
                T.HRule(w, 1);
                int labelW = w[0] + w[1] + w[2] + 6;
                string label = "  TOTAL REVENUE:".PadRight(labelW);
                string amt = revenue.ToString("F2").PadLeft(w[3]);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("  ");
                Console.Write(T.V);
                Console.Write(label);
                Console.Write(T.V);
                Console.Write(" " + amt + " ");
                Console.WriteLine(T.V);
                Console.ResetColor();
            }

            T.HRule(w, 2);
        }

        // ===== CART TABLE =====

        static void PrintCartTable(IEnumerable<CartItem> items, decimal subtotal)
        {
            int[] w = CartCols;
            bool[] ra = { false, true, true, true };

            Console.ForegroundColor = ConsoleColor.Green;
            T.HRule(w, 0);
            T.Row(new[] { "Item", "Qty", "Price(Rs)", "Total(Rs)" }, w, ra);
            T.HRule(w, 1);
            Console.ResetColor();

            foreach (var item in items)
                T.Row(new[]
                {
                    item.Product.Name,
                    item.Quantity.ToString(),
                    item.Product.Price.ToString("F2"),
                    item.Total.ToString("F2")       // uses CartItem.Total — ABSTRACTION
                }, w, ra);

            Console.ForegroundColor = ConsoleColor.Yellow;
            T.HRule(w, 1);
            int lblW = w[0] + w[1] + w[2] + 6;
            string lbl = "  SUBTOTAL:".PadRight(lblW);
            string sub = subtotal.ToString("F2").PadLeft(w[3]);
            Console.Write("  ");
            Console.Write(T.V);
            Console.Write(lbl);
            Console.Write(T.V);
            Console.Write(" " + sub + " ");
            Console.WriteLine(T.V);
            T.HRule(w, 2);
            Console.ResetColor();
        }

        // ===== RECEIPT =====

        static void PrintReceipt(Bill bill)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Green;

            int rw = 68;

            // ABSTRACTION: Local helper functions hide repetitive console writing
            void PrintLine(bool isTop)
            {
                Console.Write("  ");
                Console.Write(isTop ? T.TL : T.BL);
                for (int i = 0; i < rw; i++) Console.Write(T.H);
                Console.WriteLine(isTop ? T.TR : T.BR);
            }

            void PrintDiv()
            {
                Console.Write("  ");
                Console.Write(T.ML);
                for (int i = 0; i < rw; i++) Console.Write(T.H);
                Console.WriteLine(T.MR);
            }

            void PrintRow(string text)
            {
                if (text.Length > rw) text = text.Substring(0, rw);
                Console.Write("  ");
                Console.Write(T.V);
                Console.Write(text.PadRight(rw));
                Console.WriteLine(T.V);
            }

            void PrintCenter(string text)
            {
                int pad = (rw - text.Length) / 2;
                if (pad < 0) pad = 0;
                PrintRow(new string(' ', pad) + text);
            }

            void PrintLabelValue(string label, string value)
            {
                string val = "Rs." + value;
                string row = "  " + label.PadRight(rw - 2 - val.Length) + val;
                PrintRow(row);
            }

            PrintLine(true);
            PrintCenter("FRESH MART");
            PrintCenter("Your Neighborhood Store");
            PrintDiv();
            PrintRow("  Bill No  : " + bill.BillNo);
            PrintRow("  Date     : " + bill.Date.ToString("dd/MM/yyyy  HH:mm"));
            PrintRow("  Customer : " + bill.CustomerName);
            PrintDiv();
            PrintRow("  " + "Item".PadRight(30) + "Qty".PadLeft(5) + "  " + "Price".PadLeft(9) + "  " + "Total".PadLeft(10));
            PrintDiv();

            // COMPOSITION in action: accessing bill.Items (list of CartItems) and their nested Product
            foreach (var item in bill.Items)
            {
                string name = item.Product.Name.Length > 30 ? item.Product.Name.Substring(0, 29) + "." : item.Product.Name;
                string row = "  " + name.PadRight(30)
                                   + item.Quantity.ToString().PadLeft(5) + "  "
                                   + item.Product.Price.ToString("F2").PadLeft(9) + "  "
                                   + item.Total.ToString("F2").PadLeft(10);  // ABSTRACTION: Total auto-calculated
                PrintRow(row);
            }

            PrintDiv();
            PrintLabelValue("  Subtotal:", bill.Subtotal.ToString("F2"));
            PrintLabelValue("  Tax (5%):", bill.Tax.ToString("F2"));
            PrintLabelValue("  TOTAL:   ", bill.Total.ToString("F2"));
            PrintLabelValue("  Paid:    ", bill.Paid.ToString("F2"));
            PrintLabelValue("  Change:  ", bill.Change.ToString("F2"));
            PrintDiv();
            PrintCenter("THANK YOU FOR SHOPPING AT FRESH MART!");
            PrintCenter("VISIT AGAIN!");
            PrintLine(false);
            Console.ResetColor();
        }

        // ===== MENUS =====

        static void ShowMainMenu()
        {
            string[] opts = { "Admin Login", "Cashier Login", "Customer Login", "Customer Register", "Exit" };
            int ch = Menu("MAIN MENU", opts);
            if (ch == 0) AdminLogin();
            else if (ch == 1) CashierLogin();
            else if (ch == 2) CustomerLogin();
            else if (ch == 3) CustomerRegister();
            else Environment.Exit(0);
        }

        // ===== ADMIN =====

        static void AdminLogin()
        {
            DrawHeader("ADMIN LOGIN");
            string user = GetInput("Username");
            Console.Write("  Password: ");
            string pass = ReadPassword();
            var admin = users.FirstOrDefault(u => u.Username == user && u.Password == pass && u.Role == "Admin");
            if (admin != null) { loggedInUser = admin; ShowSuccess("Welcome " + admin.Name + "!"); Pause(); }
            else { ShowError("Invalid credentials!"); Pause(); }
        }

        static void ShowAdminPanel()
        {
            string[] opts = { "View Products", "Add Product", "Edit Product", "Delete Product", "Update Stock", "Sales Report", "Logout" };
            int ch = Menu("ADMIN PANEL", opts);
            if (ch == 0) { ViewProducts(); Pause(); }
            else if (ch == 1) AddProduct();
            else if (ch == 2) EditProduct();
            else if (ch == 3) DeleteProduct();
            else if (ch == 4) UpdateStock();
            else if (ch == 5) SalesReport();
            else loggedInUser = null;
        }

        static void AddProduct()
        {
            DrawHeader("ADD PRODUCT");
            int newId = products.Any() ? products.Max(p => p.ProductId) + 1 : 1;
            // ENCAPSULATION: Creating a Product object — all data goes through the class
            var p = new Product
            {
                ProductId = newId,
                Name = GetInput("Product Name"),
                Category = GetInput("Category"),
                Price = GetDecimal("Price"),
                Stock = GetInt("Stock Quantity")
            };
            DbAddProduct(p);
            ShowSuccess("Product '" + p.Name + "' added to MongoDB!");
            Pause();
        }

        static void EditProduct()
        {
            DrawHeader("EDIT PRODUCT");
            ViewProducts();
            int id = GetInt("\n  Product ID to edit");
            var p = products.FirstOrDefault(x => x.ProductId == id);
            if (p == null) { ShowError("Product not found!"); Pause(); return; }
            ShowInfo("Leave blank to keep current value.");

            string n = GetInput("Name (current: " + p.Name + ")");
            if (!string.IsNullOrWhiteSpace(n)) p.Name = n;

            string c = GetInput("Category (current: " + p.Category + ")");
            if (!string.IsNullOrWhiteSpace(c)) p.Category = c;

            string pr = GetInput("Price (current: Rs." + p.Price + ")");
            if (!string.IsNullOrWhiteSpace(pr))
            {
                if (decimal.TryParse(pr, out decimal dp)) p.Price = dp;
                else ShowError("Invalid price! Keeping current.");
            }

            string st = GetInput("Stock (current: " + p.Stock + ")");
            if (!string.IsNullOrWhiteSpace(st))
            {
                if (int.TryParse(st, out int ds)) p.Stock = ds;
                else ShowError("Invalid stock! Keeping current.");
            }

            DbUpdateProduct(p);
            ShowSuccess("Product '" + p.Name + "' updated!");
            Pause();
        }

        static void DeleteProduct()
        {
            DrawHeader("DELETE PRODUCT");
            ViewProducts();
            int id = GetInt("\n  Product ID to delete");
            var p = products.FirstOrDefault(x => x.ProductId == id);
            if (p == null) { ShowError("Product not found!"); Pause(); return; }
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n  WARNING: About to delete '" + p.Name + "'");
            Console.ResetColor();
            string confirm = GetInput("Type YES to confirm");
            if (confirm.ToUpper() == "YES") { DbDeleteProduct(p.ProductId); ShowSuccess("Product deleted!"); }
            else ShowInfo("Deletion cancelled.");
            Pause();
        }

        static void UpdateStock()
        {
            DrawHeader("UPDATE STOCK");
            ViewProducts();
            int id = GetInt("\n  Product ID");
            var p = products.FirstOrDefault(x => x.ProductId == id);
            if (p == null) { ShowError("Product not found!"); Pause(); return; }
            ShowInfo("Current Stock: " + p.Stock);
            int ns = GetInt("New Stock Quantity");
            if (ns >= 0) { p.Stock = ns; DbUpdateProduct(p); ShowSuccess(p.Name + ": stock = " + p.Stock); }
            else ShowError("Stock cannot be negative!");
            Pause();
        }

        static void SalesReport()
        {
            DrawHeader("SALES REPORT");
            if (!bills.Any()) { ShowInfo("No sales yet!"); Pause(); return; }
            PrintBillTable(bills, showTotal: true);
            Pause();
        }

        // ===== CASHIER =====

        static void CashierLogin()
        {
            DrawHeader("CASHIER LOGIN");
            string user = GetInput("Username");
            Console.Write("  Password: ");
            string pass = ReadPassword();
            var cashier = users.FirstOrDefault(u => u.Username == user && u.Password == pass && u.Role == "Cashier");
            if (cashier != null) { loggedInUser = cashier; ShowSuccess("Welcome " + cashier.Name + "!"); Pause(); }
            else { ShowError("Invalid credentials!"); Pause(); }
        }

        static void ShowCashierPanel()
        {
            string[] opts = { "New Billing", "View Products", "Today's Bills", "Logout" };
            int ch = Menu("CASHIER PANEL", opts);
            if (ch == 0) ProcessBilling();
            else if (ch == 1) { ViewProducts(); Pause(); }
            else if (ch == 2) ViewTodayBills();
            else loggedInUser = null;
        }

        static void ProcessBilling()
        {
            cart.Clear();
            DrawHeader("NEW BILLING");
            string custName = GetInput("Customer Name");
            if (string.IsNullOrWhiteSpace(custName)) custName = "Walk-in Customer";
            RunBillingLoop(custName);
            if (!cart.Any()) { ShowError("Cart is empty!"); Pause(); return; }
            Bill? bill = Checkout(custName);
            if (bill == null) return;
            PrintReceipt(bill);
            cart.Clear();
            ShowSuccess("Bill #" + bill.BillNo + " saved to MongoDB!");
            Pause();
        }

        static void ViewTodayBills()
        {
            DrawHeader("TODAY'S BILLS");
            var today = bills.Where(b => b.Date.Date == DateTime.Now.Date).ToList();
            if (!today.Any()) { ShowInfo("No bills today!"); Pause(); return; }
            PrintBillTable(today);
            Pause();
        }

        // ===== CUSTOMER =====

        static void CustomerRegister()
        {
            DrawHeader("CUSTOMER REGISTRATION");
            string uname = GetInput("Choose Username");
            if (users.Any(u => u.Username == uname)) { ShowError("Username already exists!"); Pause(); return; }
            // ENCAPSULATION: All user data is set through properties, not raw fields
            var nu = new User
            {
                UserId = nextUserId++,
                Name = GetInput("Full Name"),
                Phone = GetInput("Phone Number"),
                Username = uname,
                Password = "",
                Role = "Customer",   // POLYMORPHISM: Role determines behaviour at login
                RegDate = DateTime.Now
            };
            Console.Write("  Password: ");
            nu.Password = ReadPassword();
            DbAddUser(nu);
            ShowSuccess("Registered! Welcome " + nu.Name + "!");
            Pause();
        }

        static void CustomerLogin()
        {
            DrawHeader("CUSTOMER LOGIN");
            string user = GetInput("Username");
            Console.Write("  Password: ");
            string pass = ReadPassword();
            var cust = users.FirstOrDefault(u => u.Username == user && u.Password == pass && u.Role == "Customer");
            if (cust != null) { loggedInUser = cust; ShowSuccess("Welcome " + cust.Name + "!"); Pause(); }
            else { ShowError("Invalid credentials! Please register first."); Pause(); }
        }

        static void ShowCustomerPanel()
        {
            string[] opts = { "Start Shopping", "View Products", "Search Product", "Purchase History", "Logout" };
            int ch = Menu("WELCOME " + (loggedInUser?.Name.ToUpper() ?? ""), opts);
            if (ch == 0) CustomerShopping();
            else if (ch == 1) { ViewProducts(); Pause(); }
            else if (ch == 2) SearchProduct();
            else if (ch == 3) ViewPurchaseHistory();
            else loggedInUser = null;
        }

        static void CustomerShopping()
        {
            cart.Clear();
            string name = loggedInUser?.Name ?? "Customer";
            RunBillingLoop(name);
            if (!cart.Any()) { ShowError("Cart is empty!"); Pause(); return; }
            Bill? bill = Checkout(name);
            if (bill == null) return;
            PrintReceipt(bill);
            cart.Clear();
            ShowSuccess("Thank you! Bill #" + bill.BillNo + " saved!");
            Pause();
        }

        static void SearchProduct()
        {
            DrawHeader("SEARCH PRODUCT");
            string kw = GetInput("Enter product name");
            var results = products.Where(p => p.Name.ToLower().Contains(kw.ToLower())).ToList();
            if (results.Any()) PrintProductTable(results);
            else ShowError("No products found!");
            Pause();
        }

        static void ViewPurchaseHistory()
        {
            DrawHeader("PURCHASE HISTORY");
            string name = loggedInUser?.Name ?? "";
            var myBills = bills.Where(b => b.CustomerName == name).ToList();
            if (!myBills.Any()) { ShowInfo("No purchase history yet!"); Pause(); return; }
            PrintBillTable(myBills);
            Pause();
        }

        // ===== SHARED BILLING LOGIC =====

        // ABSTRACTION: One method handles billing for both Cashier and Customer
        static void RunBillingLoop(string customerName)
        {
            while (true)
            {
                Console.Clear();
                ShowLogo();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n  BILLING  |  Customer: " + customerName + "  |  Items: " + cart.Count);
                Console.WriteLine(new string('-', 74));
                Console.ResetColor();

                if (cart.Any())
                    PrintCartTable(cart, cart.Sum(i => i.Total));

                PrintProductTable(products.Where(p => p.Stock > 0).Take(10));

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("\n  Enter Product ID (0 = checkout): ");
                Console.ResetColor();

                if (!int.TryParse(Console.ReadLine(), out int pid)) continue;
                if (pid == 0) break;

                var prod = products.FirstOrDefault(p => p.ProductId == pid);
                if (prod == null) { ShowError("Product not found!"); Pause(); continue; }
                if (prod.Stock <= 0) { ShowError("Out of stock!"); Pause(); continue; }

                int qty = GetInt("Quantity (Max " + prod.Stock + ")");
                if (qty <= 0 || qty > prod.Stock) { ShowError("Invalid quantity!"); Pause(); continue; }

                var existing = cart.FirstOrDefault(c => c.Product.ProductId == pid);
                if (existing != null) existing.Quantity += qty;
                else cart.Add(new CartItem { Product = prod, Quantity = qty });  // COMPOSITION: CartItem wraps Product

                ShowSuccess("Added " + qty + " x " + prod.Name);
                Pause();
            }
        }

        // ABSTRACTION: All payment and bill-saving logic is hidden here
        static Bill? Checkout(string customerName)
        {
            decimal subtotal = cart.Sum(i => i.Total);  // uses CartItem.Total — ABSTRACTION
            decimal tax = Math.Round(subtotal * 0.05m, 2);
            decimal total = subtotal + tax;

            DrawHeader("PAYMENT");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n  Subtotal : Rs." + subtotal.ToString("F2"));
            Console.WriteLine("  Tax (5%) : Rs." + tax.ToString("F2"));
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  TOTAL    : Rs." + total.ToString("F2"));
            Console.ResetColor();

            decimal paid = GetDecimal("Payment Amount");
            if (paid < total) { ShowError("Insufficient payment!"); Pause(); return null; }

            decimal change = Math.Round(paid - total, 2);
            if (change > 0) ShowSuccess("Change: Rs." + change.ToString("F2"));

            // ENCAPSULATION: All bill data is packaged inside one Bill object
            var bill = new Bill
            {
                BillNo = nextBillNo++,
                Date = DateTime.Now,
                CustomerName = customerName,
                Items = new List<CartItem>(cart),  // COMPOSITION: Bill stores CartItems
                Subtotal = subtotal,
                Tax = tax,
                Total = total,
                Paid = paid,
                Change = change
            };

            foreach (var item in cart)
            {
                var p = products.First(x => x.ProductId == item.Product.ProductId);
                p.Stock -= item.Quantity;
                DbUpdateProduct(p);
            }

            DbAddBill(bill);
            return bill;
        }
    }
}