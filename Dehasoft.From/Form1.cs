using Dehasoft.Business.Services;
using Dehasoft.DataAccess.Models;
using Dehasoft.DataAccess.Repositories;

namespace Dehasoft.WinForms
{
    public partial class Form1 : Form
    {
        private readonly IOrderService _orderService;
        private readonly IProductRepository _productRepository;
        private readonly IProductService _productService;

        public Form1(IOrderService orderService, IProductRepository productRepository, IProductService productService)
        {
            InitializeComponent();
            _orderService = orderService;
            _productRepository = productRepository;
            _productService = productService;
        }

        private async void btnSync_Click(object sender, EventArgs e)
        {
            btnSync.Enabled = false;
            try
            {
                lblStatus.Text = "��lem devam ediyor...";
                await _orderService.FetchAndProcessOrdersAsync(1, 10);
                lblStatus.Text = "Sipari� senkronizasyonu tamamland�.";
                MessageBox.Show("Sipari�ler ba�ar�yla senkronize edildi.");
                await LoadProductsAsync();
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Bir hata olu�tu.";
                MessageBox.Show("Hata: " + ex.Message);
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
                MessageBox.Show("L�tfen bir �r�n se�in.");
                return;
            }

            var selectedProduct = (Product)dgvProducts.SelectedRows[0].DataBoundItem;
            var quantity = (int)numQuantity.Value;

            if (selectedProduct.Stock < quantity)
            {
                MessageBox.Show("Yetersiz stok.");
                return;
            }

            var orderItem = new OrderItem
            {
                ProductId = selectedProduct.Id,
                Quantity = quantity,
                SalePrice = selectedProduct.Price
            };

            var success = await _productService.ProcessOrderItemAsync(orderItem);

            if (success)
            {
                MessageBox.Show("Sat�n alma ba�ar�l�.\n�r�n: " + selectedProduct.Name +
                    "\nMiktar: " + quantity +
                    "\nKalan Stok: " + (selectedProduct.Stock - quantity));
                await LoadProductsAsync();
            }
            else
            {
                MessageBox.Show("��lem s�ras�nda hata olu�tu.\n�r�n: " + selectedProduct.Name);
            }
        }
    }
}