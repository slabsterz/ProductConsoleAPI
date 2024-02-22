using Microsoft.EntityFrameworkCore;
using ProductConsoleAPI.Business;
using ProductConsoleAPI.Business.Contracts;
using ProductConsoleAPI.Data.Models;
using ProductConsoleAPI.DataAccess;
using System.ComponentModel.DataAnnotations;

namespace ProductConsoleAPI.IntegrationTests.NUnit
{
    public class IntegrationTests
    {
        private TestProductsDbContext dbContext;
        private IProductsManager productsManager;

        [SetUp]
        public void SetUp()
        {
            this.dbContext = new TestProductsDbContext();
            this.productsManager = new ProductsManager(new ProductsRepository(this.dbContext));
        }


        [TearDown]
        public void TearDown()
        {
            this.dbContext.Database.EnsureDeleted();
            this.dbContext.Dispose();
        }


        [Test]
        public async Task AddProductAsync_ShouldAddNewProduct()
        {
            var newProduct = new Product()
            {
                OriginCountry = "Bulgaria",
                ProductName = "TestProduct",
                ProductCode = "AB12C",
                Price = 1.25m,
                Quantity = 100,
                Description = "Anything for description"
            };

            await productsManager.AddAsync(newProduct);

            var dbProduct = await this.dbContext.Products.FirstOrDefaultAsync(p => p.ProductCode == newProduct.ProductCode);

            Assert.NotNull(dbProduct);
            Assert.AreEqual(newProduct.ProductName, dbProduct.ProductName);
            Assert.AreEqual(newProduct.Description, dbProduct.Description);
            Assert.AreEqual(newProduct.Price, dbProduct.Price);
            Assert.AreEqual(newProduct.Quantity, dbProduct.Quantity);
            Assert.AreEqual(newProduct.OriginCountry, dbProduct.OriginCountry);
            Assert.AreEqual(newProduct.ProductCode, dbProduct.ProductCode);
        }

        //Negative test
        [Test]
        public async Task AddProductAsync_TryToAddProductWithInvalidCredentials_ShouldThrowException()
        {
            var newProduct = new Product()
            {
                OriginCountry = "Bulgaria",
                ProductName = "TestProduct",
                ProductCode = "AB12C",
                Price = -1m,
                Quantity = 100,
                Description = "Anything for description"
            };

            var ex = Assert.ThrowsAsync<ValidationException>(async () => await productsManager.AddAsync(newProduct));
            var actual = await dbContext.Products.FirstOrDefaultAsync(c => c.ProductCode == newProduct.ProductCode);

            Assert.IsNull(actual);
            Assert.That(ex?.Message, Is.EqualTo("Invalid product!"));

        }

        [Test]
        public async Task DeleteProductAsync_WithValidProductCode_ShouldRemoveProductFromDb()
        {
            // Arrange
            var product = new Product()
            {
                ProductCode = "1234AbcD",
                Id = 100,
                ProductName = "Random product",
                Quantity = 5,
                Price = 25.50m,
                OriginCountry = "Croatia",
                Description = "Some descritpion"
            };

            await productsManager.AddAsync(product);

            // Act
            await productsManager.DeleteAsync(product.ProductCode);
            var productInDb = await dbContext.Products.FirstOrDefaultAsync(x => x.ProductCode == product.ProductCode);

            // Assert
            Assert.Null(productInDb);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public async Task DeleteProductAsync_TryToDeleteWithNullOrWhiteSpaceProductCode_ShouldThrowException(string code)
        {
            // Arrange
            string expectedMessage = "Product code cannot be empty.";

            // Act & Assert
            var exception = Assert.ThrowsAsync<ArgumentException>(async () => await productsManager.DeleteAsync(code));
            Assert.That(exception.Message, Is.EqualTo(expectedMessage));
        }

        [Test]
        public async Task GetAllAsync_WhenProductsExist_ShouldReturnAllProducts()
        {
            // Arrange
            var productOne = new Product()
            {
                ProductCode = "1234AbcD",
                Id = 100,
                ProductName = "Random product",
                Quantity = 5,
                Price = 25.50m,
                OriginCountry = "Croatia",
                Description = "Some descritpion"
            };

            var productTwo = new Product()
            {
                ProductCode = "9886ZXC",
                Id = 256,
                ProductName = "Second product",
                Quantity = 26,
                Price = 12.90m,
                OriginCountry = "Belgium",
                Description = "Different description"
            };

            await productsManager.AddAsync(productOne);
            await productsManager.AddAsync(productTwo);

            // Act
            var result = await productsManager.GetAllAsync();
            var firstProductInDb = result.FirstOrDefault( x => x.Id == productOne.Id);
            var secondProductInDb = result.FirstOrDefault( x => x.Id == productTwo.Id);

            // Assert
            Assert.NotNull(result);
            Assert.That(result.Count(), Is.EqualTo(2));
            Assert.AreEqual(firstProductInDb.ProductCode, productOne.ProductCode);
            Assert.AreEqual(secondProductInDb.ProductCode, productTwo.ProductCode);

        }

        [Test]
        public async Task GetAllAsync_WhenNoProductsExist_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            string expectedMessage = "No product found.";
            var expected = await dbContext.Products.FirstOrDefaultAsync();

            // Act & Assert
            var exception = Assert.ThrowsAsync<KeyNotFoundException>(async () => await productsManager.GetAllAsync());
            Assert.That(exception.Message, Is.EqualTo(expectedMessage));            
            Assert.IsNull(expected);

        }

        [Test]
        public async Task SearchByOriginCountry_WithExistingOriginCountry_ShouldReturnMatchingProducts()
        {
            // Arrange
            var productOne = new Product()
            {
                ProductCode = "1234AbcD",
                Id = 100,
                ProductName = "Random product",
                Quantity = 5,
                Price = 25.50m,
                OriginCountry = "Croatia",
                Description = "Some descritpion"
            };

            var productTwo = new Product()
            {
                ProductCode = "9886ZXC",
                Id = 256,
                ProductName = "Second product",
                Quantity = 26,
                Price = 12.90m,
                OriginCountry = "Croatia",
                Description = "Different description"
            };

            string originCountry = "Croatia";

            await productsManager.AddAsync(productOne);
            await productsManager.AddAsync(productTwo);

            // Act
            var result = await productsManager.SearchByOriginCountry(originCountry);
            var productsInDb = result.Where( x => x.OriginCountry == originCountry);   
            
            var firstElementInResult = result.First();
            var secondelementInResult = result.ElementAtOrDefault(1);

            var firstElementInDb = productsInDb.First();
            var secondelementInDb = productsInDb.ElementAtOrDefault(1);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(productsInDb);
            Assert.That(result.Count(), Is.EqualTo(productsInDb.Count()));
            Assert.That(firstElementInResult.OriginCountry, Is.EqualTo(firstElementInDb.OriginCountry));
            Assert.That(secondelementInResult.OriginCountry, Is.EqualTo(secondelementInDb.OriginCountry));
            Assert.That(secondelementInDb.OriginCountry, Is.EqualTo(firstElementInDb.OriginCountry));
            Assert.That(firstElementInResult.OriginCountry, Is.EqualTo(originCountry));
            Assert.That(secondelementInResult.OriginCountry, Is.EqualTo(originCountry));
            Assert.That(secondelementInResult.OriginCountry, Is.EqualTo(firstElementInResult.OriginCountry));
            Assert.That(secondelementInResult.ProductCode, Is.Not.EqualTo(firstElementInResult.ProductCode));            
        }

        [Test]
        public async Task SearchByOriginCountryAsync_WithNonExistingOriginCountry_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            string expectedMessage = "No product found with the given first name.";
            string nonExistingOrigin = "Latvia";
            var expected = await dbContext.Products.FirstOrDefaultAsync();

            // Act & Assert
            var exception = Assert.ThrowsAsync<KeyNotFoundException>(async () => await productsManager.SearchByOriginCountry(nonExistingOrigin));
            Assert.IsNull(expected);
            Assert.That(exception.Message, Is.EqualTo(expectedMessage));

        }

        [Test]
        public async Task GetSpecificAsync_WithValidProductCode_ShouldReturnProduct()
        {
            // Arrange
            var product = new Product()
            {
                ProductCode = "1234AbcD",
                Id = 100,
                ProductName = "Random product",
                Quantity = 5,
                Price = 25.50m,
                OriginCountry = "Croatia",
                Description = "Some descritpion"
            };

            await productsManager.AddAsync(product);

            // Act
            var result = await productsManager.GetSpecificAsync(product.ProductCode);

            // Assert
            Assert.NotNull(result);
            Assert.That(result.ProductCode, Is.EqualTo(product.ProductCode));
        }

        [Test]
        public async Task GetSpecificAsync_WithInvalidProductCode_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            string invalidProductCode = "nonExisting";
            string exceptionMessage = $"No product found with product code: {invalidProductCode}";

            // Act & Assert
            var result = Assert.ThrowsAsync<KeyNotFoundException>( async () => await productsManager.GetSpecificAsync(invalidProductCode));
            Assert.That(result.Message, Is.EqualTo(exceptionMessage));
        }

        [Test]
        public async Task UpdateAsync_WithValidProduct_ShouldUpdateProduct()
        {
            // Arrange
            string initialName = "Random product";

            var product = new Product()
            {
                ProductCode = "1234AbcD",
                Id = 100,
                ProductName = initialName,
                Quantity = 5,
                Price = 25.50m,
                OriginCountry = "Croatia",
                Description = "Some descritpion"
            };

            await productsManager.AddAsync(product);

            string updatedName = "New Name";
            product.ProductName = updatedName;

            // Act
            await productsManager.UpdateAsync(product);
            var productInDb = await dbContext.Products.FirstAsync();

            // Assert
            Assert.NotNull(productInDb);
            Assert.That(productInDb.ProductName, Is.EqualTo(updatedName));
            Assert.That(productInDb.ProductName, Is.Not.EqualTo(initialName));
        }

        [Test]
        public async Task UpdateAsync_WithInvalidProduct_ShouldThrowValidationException()
        {
            // Arrange
            string errorMessage = "Invalid prduct!";

            // Act & Assert        
            var result = Assert.ThrowsAsync<ValidationException>(async () => await productsManager.UpdateAsync(new Product()));
            Assert.That(result.Message, Is.EqualTo(errorMessage));
        }
    }
}
