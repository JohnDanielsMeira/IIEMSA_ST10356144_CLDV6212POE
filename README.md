# IIEMSA_ST10356144_CLDV6212POE
Published Link:
https://st10356144abcretail-habpcheshbf0cffg.canadacentral-01.azurewebsites.net/
Video Link:
https://www.loom.com/share/c46a2716b54042859382d055a5073e52?sid=da93f17b-41da-40d7-8129-8517b3bf670d

ABCRetailers is company that sells clothing ad other accessories online. I created a web app that will allow the users to add new customers, add new products -the price and quantity of the stock, add orders where users can select from the list of products that have been added and at any quantity less than tptal stock. I have created an uploads page where users can upload pdfs, pictures or word documents of their proof of payments or contracts. All input is stored using Azure Storage Account, within the account are tables where is of the data is stored, blob containers where pictures and other multimedia are stored, queues where messages for orders are stored and file shares where the proof of payments and contracts are stored. I made use of models (Customer, Product, Order, FileUploadModel, ErrorViewModel, HomeViewModel, OrderCreateViewModel), Controllers(HomeController, CustomerController, ProductController, OrderController, UploadController), Views (Shared, Home, Customer, Product, Order, Upload) and Services (IAzureStorageService, AzureStorageService)
