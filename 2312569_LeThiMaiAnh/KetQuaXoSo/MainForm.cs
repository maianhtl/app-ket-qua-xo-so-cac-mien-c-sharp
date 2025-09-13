using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KetQuaXoSo
{
    public partial class MainForm : Form
    {
        private class NodeData
        {
            public string Province { get; set; }
            public XoSoHelper.KetQuaXoSo KetQua { get; set; }
            public string RssLink { get; set; }
            public string RssCode { get; set; }
        }

        public MainForm()
        {
            InitializeComponent();

            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = true;
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            HideCheckUI();

            string[] provinces =
            {
                "Miền Bắc","An Giang","Bình Dương","Bình Định","Bạc Liêu","Bình Phước","Bến Tre","Bình Thuận",
                "Cà Mau","Cần Thơ","Đắk Lắk","Đồng Nai","Đà Nẵng","Đắk Nông","Đồng Tháp","Gia Lai","Hồ Chí Minh",
                "Hậu Giang","Kiên Giang","Khánh Hòa","Kon Tum","Long An","Lâm Đồng","Ninh Thuận","Phú Yên",
                "Quảng Bình","Quảng Ngãi","Quảng Nam","Quảng Trị","Sóc Trăng","Tiền Giang","Tây Ninh",
                "Thừa Thiên Huế","Trà Vinh","Vĩnh Long","Vũng Tàu"
            };

            treeView1.Nodes.Clear();

            foreach (string province in provinces)
            {
                TreeNode parentNode = new TreeNode(province);
                parentNode.Tag = province;
                parentNode.Nodes.Add("Đang tải...");
                treeView1.Nodes.Add(parentNode);
            }

            treeView1.BeforeExpand += async (s, ev) =>
            {
                TreeNode selectedNode = ev.Node;
                if (selectedNode.Nodes.Count == 1 && selectedNode.Nodes[0].Text == "Đang tải...")
                {
                    selectedNode.Nodes.Clear();
                    string province = selectedNode.Tag.ToString();
                    string code = XoSoHelper.Program.CreateCodeRSS(province);
                    string rss = XoSoHelper.Program.CreateRSSLink(province, code);
                    var ketquas = await XoSoHelper.XoSoParser.ParseRss(rss);

                    if (ketquas.Count == 0)
                    {
                        selectedNode.Nodes.Add("Không có dữ liệu");
                        return;
                    }

                    foreach (var kq in ketquas.OrderByDescending(k => k.NgayQuay))
                    {
                        TreeNode dateNode = new TreeNode(kq.NgayQuay.ToString("dd/MM/yyyy"));
                        dateNode.Tag = new NodeData
                        {
                            Province = province,
                            KetQua = kq,
                            RssLink = rss,
                            RssCode = code
                        };
                        selectedNode.Nodes.Add(dateNode);
                    }
                }
            };

            treeView1.AfterSelect += (s, ev) =>
            {
                if (ev.Node.Tag == null)
                {
                    HideCheckUI();
                    return;
                }

                if (ev.Node.Tag is string provinceName)
                {
                    textBox1.Text = "Bạn đã chọn đài " + provinceName + ". Hãy mở node để xem các ngày có kết quả.";
                    HideCheckUI();
                    return;
                }

                if (ev.Node.Tag is NodeData data)
                {
                    var kq = data.KetQua;
                    var sb = new StringBuilder();
                    sb.AppendLine("Kết quả " + data.Province + " - ngày " + kq.NgayQuay.ToString("dd/MM/yyyy"));
                    sb.AppendLine("Đặc biệt: " + kq.DacBiet);
                    sb.AppendLine("Giải 1: " + kq.Giai1);
                    sb.AppendLine("Giải 2: " + kq.Giai2);
                    sb.AppendLine("Giải 3: " + string.Join(", ", kq.Giai3));
                    sb.AppendLine("Giải 4: " + string.Join(", ", kq.Giai4));
                    sb.AppendLine("Giải 5: " + kq.Giai5);
                    sb.AppendLine("Giải 6: " + string.Join(", ", kq.Giai6));
                    sb.AppendLine("Giải 7: " + string.Join(", ", kq.Giai7));
                    if (kq.CoGiai8)
                        sb.AppendLine("Giải 8: " + kq.Giai8);

                    textBox1.Text = sb.ToString();

                    ShowCheckUI();
                    linkLabel1.Text = "Xem thêm ...";
                    linkLabel1.Tag = "https://xskt.com.vn/xs" + data.RssCode;
                }
            };

            linkLabel1.LinkClicked += (s, ev) =>
            {
                if (linkLabel1.Tag is string url)
                {
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = url,
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Không mở được link: " + ex.Message);
                    }
                }
            };
        }

        private void txtKT_Click(object sender, EventArgs e)
        {
            string soVe = txtVeSo.Text;
            if (string.IsNullOrWhiteSpace(soVe) || soVe.Length != 6 || !soVe.All(char.IsDigit))
            {
                MessageBox.Show("Vui lòng nhập số vé hợp lệ (6 chữ số).");
                return;
            }
            if (treeView1.SelectedNode != null && treeView1.SelectedNode.Tag is NodeData data)
            {
                var kq = data.KetQua;
                var ketQuaTrung = XoSoHelper.Program.KiemTraVeSo(soVe, kq);
                txtKQ.Text = ketQuaTrung;
            }
            else
            {
                MessageBox.Show("Vui lòng chọn ngày quay số để kiểm tra.");
            }
        }

        private void HideCheckUI()
        {
            txtVeSo.Visible = false;
            txtKT.Visible = false;
            txtKQ.Visible = false;
            label1.Visible = false;
            linkLabel1.Visible = false;
        }

        private void ShowCheckUI()
        {
            txtVeSo.Visible = true;
            txtKT.Visible = true;
            txtKQ.Visible = true;
            label1.Visible = true;
            linkLabel1.Visible = true;
        }
    }
}
