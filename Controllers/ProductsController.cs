using ComputerHardwareOnlineStore.Models;
using CsvHelper;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;
using System.Formats.Asn1;
using System.Globalization;
using System.Text; 

namespace ComputerHardwareOnlineStore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly string connectionString;
        public ProductsController(IConfiguration configuration) 
        {
            connectionString = configuration["ConnectionStrings:SqlServerDb"] ?? "";
        }
        [HttpPost("upload")]
        public IActionResult UploadProducts(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("File is empty");
            }

            List<ProductDto> products = new List<ProductDto>();

            try
            {
                if (file.Length > 0)
                {
                    using (var streamReader = new StreamReader(file.OpenReadStream()))
                    using (var csvReader = new CsvReader(streamReader, CultureInfo.InvariantCulture))
                    {
                        var records = csvReader.GetRecords<ProductDto>().ToList();
                        products.AddRange(records);
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"An error occurred: {ex.Message}");
            }

            //if (products.Count < 20000)
            //{
            //    return BadRequest("Minimum 20,000 products required");
            //}

            // Bulk insert products into database using transactions
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var sql = "INSERT INTO products (Name, Description, Qty, Amount, InStock) VALUES ";
                        var stringBuilder = new StringBuilder(sql);

                        for (int i = 0; i < products.Count; i++)
                        {
                            stringBuilder.Append($"(@Name{i}, @Description{i}, @Qty{i}, @Amount{i}, @InStock{i})");
                            if (i < products.Count - 1)
                            {
                                stringBuilder.Append(", ");
                            }
                        }

                        var finalSql = stringBuilder.ToString();
                        var command = new SqlCommand(finalSql, connection, transaction);

                        for (int i = 0; i < products.Count; i++)
                        {
                            command.Parameters.AddWithValue($"@Name{i}", products[i].Name);
                            command.Parameters.AddWithValue($"@Description{i}", products[i].Description);
                            command.Parameters.AddWithValue($"@Qty{i}", products[i].Qty);
                            command.Parameters.AddWithValue($"@Amount{i}", products[i].Amount);
                            command.Parameters.AddWithValue($"@InStock{i}", products[i].InStock);
                        }

                        command.ExecuteNonQuery();
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return BadRequest($"An error occurred: {ex.Message}");
                    }
                }
            }

            return Ok("Products uploaded successfully");
        }
        [HttpGet]
        public IActionResult ListProducts(string? searchTerm = null)
        {
            List<ProductDto> products = new List<ProductDto>();

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string sql = "SELECT Name, Description, Qty, Amount, InStock FROM products";
                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        sql += " WHERE Name LIKE '%' + @SearchTerm + '%'";
                    }

                    using (var command = new SqlCommand(sql, connection))
                    {
                        if (!string.IsNullOrEmpty(searchTerm))
                        {
                            command.Parameters.AddWithValue("@SearchTerm", searchTerm);
                        }

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var product = new ProductDto
                                {
                                    Name = reader.GetString(0),
                                    Description = reader.GetString(1),
                                    Qty = reader.GetInt32(2),
                                    Amount = reader.GetDecimal(3),
                                    InStock = reader.GetInt32(4)
                                };
                                products.Add(product);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest("Sorry, We have an Exception");
            }

            return Ok(products);
        }

        [HttpPost]
        public IActionResult CreateProduct(ProductDto productDto) 
        {
            try
            {
                using (var connection = new SqlConnection(connectionString)) 
                {
                    connection.Open();
                    string sql = "INSERT INTO products" +
                        "(Name,Description,Qty,Amount,InStock) VALUES" + 
                        "(@Name,@Description,@Qty,@Amount,@InStock)"; 
                    using (var command = new SqlCommand(sql,connection)) 
                    {
                        command.Parameters.AddWithValue("@Name",productDto.Name);
                        command.Parameters.AddWithValue("@Description", productDto.Description);
                        command.Parameters.AddWithValue("@Qty", productDto.Qty);
                        command.Parameters.AddWithValue("@Amount", productDto.Amount);
                        command.Parameters.AddWithValue("@InStock", productDto.InStock);

                        command.ExecuteNonQuery();
                    }

                }
            }
            catch (Exception ex) 
            {
                ModelState.AddModelError("Product","Sorry, We have an Exception");
                return BadRequest(ModelState);
            }
            return Ok();
        }
        [HttpGet("all")]
        public IActionResult GetAllProducts()
        {
            List<ProductDto> products = new List<ProductDto>();

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string sql = "SELECT * FROM products";
                    using (var command = new SqlCommand(sql, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ProductDto product = new ProductDto
                                {
                                    Name = reader["Name"].ToString(),
                                    Description = reader["Description"].ToString(),
                                    Qty = Convert.ToInt32(reader["Qty"]),
                                    Amount = Convert.ToDecimal(reader["Amount"]),
                                    InStock = Convert.ToInt32(reader["InStock"])
                                };
                                products.Add(product);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Product", "Sorry, We encountered an Exception while fetching products.");
                return BadRequest(ModelState);
            }

            return Ok(products);
        }

    }
}
