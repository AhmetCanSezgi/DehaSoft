using Dehasoft.Business.Services;
using Dehasoft.DataAccess.Models;
using Dehasoft.DataAccess.Repositories;
using System.Data;

namespace Dehasoft.WinForms
{
    public partial class Form1 : Form
    {
        private readonly IOrderService _orderService;
        private readonly IProductRepository _productRepository;
        private readonly IProductService _productService;
        private readonly ILogService _logService;

        private NumericUpDown? nudNewPrice;
        private Label? lblNewPrice;
        private GroupBox? groupBoxControls;

        public Form1(IOrderService orderService, IProductRepository productRepository, IProductService productService, ILogService logService)
        {
            InitializeComponent();
            _orderService = orderService;
            _productRepository = productRepository;
            _productService = productService;
            _logService = logService;

            dgvProducts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvProducts.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvProducts.MultiSelect = false;

            InitializeCustomControls();
        }

        private void InitializeCustomControls()
        {
            groupBoxControls = new GroupBox
            {
                Text = "�r�n G�ncelleme",
                Location = new Point(30, 430),
                Size = new Size(640, 80),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            lblNewPrice = new Label
            {
                Text = "Yeni Fiyat:",
                Location = new Point(20, 35),
                AutoSize = true
            };

            nudNewPrice = new NumericUpDown
            {
                DecimalPlaces = 2,
                Maximum = 100000000,
                Location = new Point(100, 30),
                Size = new Size(120, 27),
                Name = "nudNewPrice"
            };

            var btnUpdate = new Button
            {
                Text = "G�ncelle",
                Location = new Point(250, 28),
                Size = new Size(100, 30),
                Name = "btnUpdate"
            };
            btnUpdate.Click += btnUpdate_Click;

            groupBoxControls.Controls.Add(lblNewPrice);
            groupBoxControls.Controls.Add(nudNewPrice);
            groupBoxControls.Controls.Add(btnUpdate);

            Controls.Add(groupBoxControls);
        }

        private async void btnSync_Click(object sender, EventArgs e)
        {
            btnSync.Enabled = false;
            lblStatus.Text = "��lem devam ediyor...";

            try
            {
                await _orderService.FetchAndProcessOrdersAsync(1, 10);
                lblStatus.Text = "Sipari� senkronizasyonu tamamland�.";
                MessageBox.Show("Sipari�ler ba�ar�yla senkronize edildi.", "Ba�ar�l�", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await _logService.LogAsync("INFO", "[SYNC] Sipari�ler ba�ar�yla senkronize edildi.");
                await LoadProductsAsync();
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Bir hata olu�tu.";
                await _logService.LogAsync("ERROR", $"[SYNC ERROR] {ex.Message}");
                MessageBox.Show("Hata: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSync.Enabled = true;
            }
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            await LoadProductsAsync();
        }

        private async Task LoadProductsAsync()
        {
            using var conn = _productRepository.GetDbConnection();
            var products = await _productRepository.GetAllAsync(conn);
            dgvProducts.DataSource = products;
        }

        private async void btnBuy_Click(object sender, EventArgs e)
        {
            if (dgvProducts.SelectedRows.Count == 0)
            {
                MessageBox.Show("L�tfen bir �r�n se�in.", "Uyar�", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedProduct = (Product)dgvProducts.SelectedRows[0].DataBoundItem;
            var quantity = (int)numQuantity.Value;

            if (selectedProduct.Stock < quantity)
            {
                MessageBox.Show("Yetersiz stok.", "Uyar�", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var orderItem = new OrderItem
            {
                ProductId = selectedProduct.Id,
                Quantity = quantity,
                BuyPrice = selectedProduct.Price,
                SalePrice = selectedProduct.Price
            };

            var success = await _productService.ProcessOrderItemAsync(orderItem);

            if (success)
            {
                
                MessageBox.Show(
                    $"Sat�n alma ba�ar�l�!\n\n�r�n: {selectedProduct.Name}\nMiktar: {quantity}\nKalan Stok: {selectedProduct.Stock - quantity}",
                    "Ba�ar�l�", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadProductsAsync();
            }
            else
            {
                await _logService.LogAsync("ERROR", $"[BUY ERROR] {selectedProduct.Name} sat�n alma ba�ar�s�z oldu.");
                MessageBox.Show("��lem s�ras�nda hata olu�tu.\n�r�n: " + selectedProduct.Name, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btnUpdate_Click(object? sender, EventArgs e)
        {
            if (dgvProducts.SelectedRows.Count == 0)
            {
                MessageBox.Show("L�tfen bir �r�n se�in.", "Uyar�", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (nudNewPrice == null)
            {
                MessageBox.Show("G�ncelleme kontrolleri y�klenemedi.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var selectedProduct = (Product)dgvProducts.SelectedRows[0].DataBoundItem;
            var newPrice = nudNewPrice.Value;

            var success = await _productService.UpdateProductPriceAndStockAsync(selectedProduct.Id, newPrice);

            if (success)
            {
                await _logService.LogAsync("INFO", $"[UPDATE] �r�n g�ncellendi. �r�n: {selectedProduct.Name}, Yeni Fiyat: {newPrice}");
                MessageBox.Show("G�ncelleme ba�ar�l�.", "Ba�ar�l�", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadProductsAsync();
            }
            else
            {
                await _logService.LogAsync("ERROR", $"[UPDATE ERROR] �r�n g�ncelleme ba�ar�s�z. �r�n: {selectedProduct.Name}");
                MessageBox.Show("G�ncelleme ba�ar�s�z oldu.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
