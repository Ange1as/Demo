using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoWindowsFormsApp
{
    public class CostCalculator
    {
        private readonly string _connectionString;

        public CostCalculator(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        /// <summary>
        /// Рассчитывает себестоимость услуги по её ID.
        /// </summary>
        /// <param name="serviceId">Идентификатор услуги</param>
        /// <returns>Себестоимость услуги или -1, если данные не найдены</returns>
        public decimal CalculateServiceCost(int serviceId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    // 1. Получаем норму времени и квалификацию сотрудника из таблицы Services
                    string serviceQuery = @"
                    SELECT 
                        labor_hours,
                        qualification_id
                    FROM dbo.Services
                    WHERE id = @serviceId";

                    decimal laborHours = 0;
                    int? qualificationId = null;

                    using (var cmd = new SqlCommand(serviceQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@serviceId", serviceId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                return -1; // Услуга не найдена
                            }

                            laborHours = reader["labor_hours"] as decimal? ?? 0;
                            qualificationId = reader["qualification_id"] as int?;
                        }
                    }

                    if (qualificationId == null)
                    {
                        return -1; // Квалификация не указана
                    }

                    // 2. Получаем часовую ставку по qualification_id
                    decimal hourlyRate = GetHourlyRate(connection, qualificationId.Value);
                    if (hourlyRate == -1)
                    {
                        return -1;
                    }

                    // 3. Рассчитываем трудозатраты
                    decimal laborCost = laborHours * hourlyRate;

                    // 4. Рассчитываем стоимость материалов
                    decimal materialCost = CalculateMaterialCost(connection, serviceId);
                    if (materialCost == -1)
                    {
                        return -1;
                    }

                    // 5. Итоговая себестоимость
                    return laborCost + materialCost;
                }
            }
            catch (Exception ex)
            {
                // В реальном приложении — логирование
                // Console.WriteLine($"Ошибка расчёта себестоимости: {ex.Message}");
                return -1;
            }
        }

        private decimal GetHourlyRate(SqlConnection connection, int qualificationId)
        {
            string query = @"
            SELECT hourly_rate 
            FROM dbo.Qualifications 
            WHERE id = @qualificationId";

            using (var cmd = new SqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@qualificationId", qualificationId);
                var result = cmd.ExecuteScalar();
                if (result == null || result == DBNull.Value)
                {
                    return -1;
                }
                return Convert.ToDecimal(result);
            }
        }

        private decimal CalculateMaterialCost(SqlConnection connection, int serviceId)
        {
            // Запрос: сумма (норма расхода * текущая цена материала)
            string query = @"
            SELECT 
                SUM(sm.quantity * m.current_price) AS total_material_cost
            FROM dbo.ServiceMaterials sm
            INNER JOIN dbo.Materials m ON sm.material_id = m.id
            WHERE sm.service_id = @serviceId";

            using (var cmd = new SqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@serviceId", serviceId);
                var result = cmd.ExecuteScalar();

                if (result == null || result == DBNull.Value)
                {
                    return 0; // Нет материалов — стоимость 0
                }

                return Convert.ToDecimal(result);
            }
        }
    }
}
