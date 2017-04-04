using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.IO;




namespace POS_C
{
    public partial class transactionScreen : Form
    {
        Transaction transaction = new Transaction();        // Creation of initial transaction upon load

        public transactionScreen()
        {
            InitializeComponent();

            // Sets up the handling of key input in the form.
            KeyPreview = true;
            KeyDown += new KeyEventHandler(transactionScreen_KeyDown);
            CreateFileWatcher(@"C:\sku\");
        }

        private void transactionsScreen_Load(object sender, EventArgs e)
        {
            // Set DataGridView properties upon load
            inventoryDataGridView.Columns[2].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            inventoryDataGridView.Columns[2].DefaultCellStyle.Format = "c";        // currency format
        }

        // Handles hotkey presses on the form
        // F3 = New Transaction
        // F5 = Finalize
        private void transactionScreen_KeyDown(object sender, KeyEventArgs key)
        {
            switch (key.KeyCode)
            {
                case Keys.F3:
                    newTransactionButton.PerformClick();
                    break;
                case Keys.F5:
                    finalizeButton.PerformClick();
                    break;
            }
        }
        
        // Closes the Transactions form
        private void closeTransactions_Click(object sender, EventArgs e)
        {
            Close();
        }

        // Finalize the transaction
        private void finalizeButton_Click(object sender, EventArgs e)
        {
            decimal tendered;
            skuErrorLabel.Visible = false;
            tenderedErrorLabel.Visible = false;

            // Try the parsing of tendered box, display error if invalid decimal or invalid tendered amount (less than total)
            try
            {
                tendered = Decimal.Parse(amountTenderedBox.Text);
                transaction.finalize(tendered, this);
            }
            catch
            {
                // Displays the tendered error label when a tender error is caught.
                tenderedErrorLabel.Visible = true;
                amountTenderedBox.Focus();
                amountTenderedBox.SelectionStart = 0;
                amountTenderedBox.SelectionLength = amountTenderedBox.TextLength;
            }
        }

        // Hitting the Return/Enter key when in the SKU textbox.
        private void addItem_keyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Return)
            {
                long sku;                            // Stores the SKU of the item entered
                decimal price;                      // Stores the price of the item entered

                // Reset error labels
                skuErrorLabel.Visible = false;
                tenderedErrorLabel.Visible = false;

                // Try parsing of entered SKU, if failed, display an error indicating the SKU wasn't found
                try
                {
                    sku = Int64.Parse(this.skuBox.Text);
                    price = (decimal)inventoryTableAdapter.GetPrice(sku);
                    totalItemsLabel.Text = transaction.AddItem(sku, this).ToString();
                    transaction.UpdateTotals(price, this);
                    if (Int32.Parse(totalItemsLabel.Text) == 1)
                        finalizeButton.Enabled = true;
                    e.Handled = true;
                }
                catch
                {
                    // Displays an error when the user searches for a 
                    // non-SKU query (ex. anything with letters/symbols/etc.)
                    skuErrorLabel.Visible = true;
                }

                // Highlight text in skuBox
                skuBox.Focus();
                skuBox.SelectionStart = 0;
                skuBox.SelectionLength = skuBox.TextLength;
            }
        }

        // Hitting the Return/Enter key when in the Amount Tendered textbox.
        private void Tender_Enter(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Return)
            {
                e.Handled = true;
                finalizeButton.PerformClick();
            }
        }

        // Sets up the form for a new transaction.
        private void newTransactionButton_Click(object sender, EventArgs e)
        {
            transaction = new Transaction();
            resetForm();
        }

        // Resets the form.
        private void resetForm()
        {
            finalizeButton.Enabled = false;
            taxLabel.Text = "$0.00";
            skuBox.ResetText();
            totalLabel.Text = "$0.00";
            subtotalLabel.Text = "$0.00";
            changeLabel.ResetText();
            amountTenderedBox.Enabled = true;
            amountTenderedBox.ResetText();
            totalItemsLabel.Text = "0";
            changeTitleLabel.Visible = false;
            skuErrorLabel.Visible = false;
            tenderedErrorLabel.Visible = false;
            skuBox.Enabled = true;
            mASTERDataSet.Clear();
            skuBox.Focus();
            skuBox.SelectionStart = 0;
            skuBox.SelectionLength = skuBox.TextLength;
        }

        public void CreateFileWatcher(string path)
        {
            // Create a new FileSystemWatcher and set its properties.
            FileSystemWatcher watcher = new System.IO.FileSystemWatcher();
            watcher.Path = @"C:\sku";
            /* Watch for changes in LastAccess and LastWrite times, and 
               the renaming of files or directories. */
            watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
               | NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Size;
            // Only watch text files.
            watcher.Filter = "sku.txt";

            // Add event handlers.
            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.Created += new FileSystemEventHandler(OnChanged);
            watcher.Deleted += new FileSystemEventHandler(OnChanged);
            watcher.Renamed += new RenamedEventHandler(OnRenamed);

            // Begin watching.
            watcher.EnableRaisingEvents = true;
        }

        // Define the event handlers.
        private void OnChanged(object source, FileSystemEventArgs e)
        {
            // Specify what is done when a file is changed, created, or deleted.
            Console.WriteLine("File: " + e.FullPath + " " + e.ChangeType);
            try { 
                SetText(File.ReadAllText(e.FullPath));
            } catch
            {
            }

        }

        private void OnRenamed(object source, RenamedEventArgs e)
        {
            // Specify what is done when a file is renamed.
            Console.WriteLine("File: {0} renamed to {1}", e.OldFullPath, e.FullPath);
        }

        delegate void SetTextCallback(string text);

        private void SetText(string text)
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (this.skuBox.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetText);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.skuBox.Text = text;
                KeyPressEventArgs e = new KeyPressEventArgs((char)13);
                addItem_keyPress(null, e);
            }
        }

    }
}
