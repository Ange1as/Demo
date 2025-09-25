using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DemoWindowsFormsApp
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // TODO: данная строка кода позволяет загрузить данные в таблицу "korgozha_DemoProbDataSet.PartnerServices". При необходимости она может быть перемещена или удалена.
            this.partnerServicesTableAdapter.Fill(this.korgozha_DemoProbDataSet.PartnerServices);
            // TODO: данная строка кода позволяет загрузить данные в таблицу "korgozha_DemoProbDataSet.Services". При необходимости она может быть перемещена или удалена.
            this.servicesTableAdapter.Fill(this.korgozha_DemoProbDataSet.Services);
            // TODO: данная строка кода позволяет загрузить данные в таблицу "korgozha_DemoProbDataSet.Partners". При необходимости она может быть перемещена или удалена.
            this.partnersTableAdapter.Fill(this.korgozha_DemoProbDataSet.Partners);

        }

        private void сохранитьToolStripButton_Click(object sender, EventArgs e)
        {
            try
            {
                // Проверяем, есть ли изменения для сохранения
                if (korgozha_DemoProbDataSet.HasChanges())
                {
                    this.Validate();
                    this.partnersBindingSource.EndEdit();

                    // Выполняем обновление в базе данных
                    int rowsAffected = this.partnersTableAdapter.Update(this.korgozha_DemoProbDataSet.Partners);

                    if (rowsAffected > 0)
                    {
                        this.korgozha_DemoProbDataSet.Partners.AcceptChanges();
                        MessageBox.Show("Изменения успешно сохранены в базу данных.", "Успех",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("Нет изменений для сохранения.", "Информация",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    MessageBox.Show("Нет изменений для сохранения.", "Информация",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (System.Data.SqlClient.SqlException sqlEx)
            {
                // Обработка ошибок SQL Server (например, нарушение уникальности)
                string errorMessage = "Ошибка при сохранении данных в базу:\n\n";

                if (sqlEx.Number == 2627 || sqlEx.Number == 2601) // Нарушение UNIQUE или PRIMARY KEY
                {
                    errorMessage += "Невозможно сохранить запись: обнаружен дубликат уникального значения (например, имя партнёра уже существует).";
                }
                else
                {
                    errorMessage += sqlEx.Message;
                }

                MessageBox.Show(errorMessage, "Ошибка базы данных",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                // Обработка всех остальных ошибок
                MessageBox.Show($"Произошла ошибка при сохранении:\n\n{ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Проверяем, что строка выбрана
            if (partnersDataGridView.SelectedRows.Count == 0)
            {
                MessageBox.Show("Пожалуйста, выберите строку с партнером.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Получаем ID партнера из выбранной строки
            var selectedRow = partnersDataGridView.SelectedRows[0];
            int partnerId;

            // Предполагаем, что первый столбец — это ID партнера
            if (!int.TryParse(selectedRow.Cells["dataGridViewTextBoxColumn1"].Value?.ToString(), out partnerId))
            {
                MessageBox.Show("Не удалось получить ID партнера.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Получаем историю услуг для этого партнера
            string servicesList = GetPartnerServices(partnerId);

            // Выводим в сообщении
            if (string.IsNullOrEmpty(servicesList))
            {
                MessageBox.Show($"У партнера с ID {partnerId} нет оказанных услуг.", "История услуг", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show($"История услуг партнера (ID: {partnerId}):\n\n{servicesList}", "История услуг", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private string GetPartnerServices(int partnerId)
        {
            string connectionString = @"Data Source=ADCLG1;Initial Catalog=Korgozha_DemoProb;Integrated Security=True;";


            string query = @"
        SELECT
            s.service_name,
            ps.execution_date,
            ps.quantity
        FROM dbo.PartnerServices ps
        INNER JOIN dbo.Services s ON ps.service_id = s.id
        WHERE ps.partner_id = @partnerId
        ORDER BY ps.execution_date DESC";

    using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@partnerId", partnerId);
                connection.Open();

                var sb = new StringBuilder();
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.HasRows)
                    {
                        return "Нет оказанных услуг.";
                    }

                    while (reader.Read())
                    {
                        string serviceName = reader["service_name"].ToString();    
                        string executionDate = reader["execution_date"].ToString(); 
                        string quantity = reader["quantity"].ToString();            

                        sb.AppendLine($"{serviceName} | {executionDate} | Кол-во: {quantity}");
                    }
                }

                return sb.ToString();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string connectionString = @"Data Source=ADCLG1;Initial Catalog=Korgozha_DemoProb;Integrated Security=True";

            string query = @"
        SELECT 
            partner_type,
            partner_name,
            director_name,
            phone,
            rating
        FROM dbo.Partners
        ORDER BY id"; // Можно сортировать как угодно

            using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand(query, connection))
            {
                connection.Open();
                var sb = new StringBuilder();

                using (var reader = command.ExecuteReader())
                {
                    if (!reader.HasRows)
                    {
                        MessageBox.Show("В базе данных нет партнеров.", "Список партнеров", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    while (reader.Read())
                    {
                        string partnerType = reader["partner_type"].ToString();
                        string partnerName = reader["partner_name"].ToString();
                        string directorName = reader["director_name"].ToString();
                        string phone = reader["phone"].ToString();
                        string rating = reader["rating"].ToString();

                        // Формируем блок по макету
                        sb.AppendLine($"{partnerType} | {partnerName}");
                        sb.AppendLine(directorName);
                        sb.AppendLine(phone);
                        sb.AppendLine($"Рейтинг: {rating}");
                        sb.AppendLine(); // Пустая строка между блоками
                    }
                }

                // Выводим весь список
                MessageBox.Show(sb.ToString(), "Список партнеров", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
