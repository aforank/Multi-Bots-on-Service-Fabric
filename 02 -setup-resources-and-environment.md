In this lab, you will create and configure fabric resource cluster, API management resource and bot channel registration resource.

**Exercise 1: Create a Resource Group**

1. Sign in to the  [Azure portal](https://portal.azure.com/).
2. To see all the resource groups in your subscription, select  **Resource groups**

      ![alt text](https://asfabricstorage.blob.core.windows.net/lab1images/1.png)

3. To create an empty resource group, select  **Add**.

      ![alt text](https://asfabricstorage.blob.core.windows.net/lab1images/2.png)

4. Provide Resource Group Name as &quot;OneBankRG&quot;, Resource Group Location as &quot;West US&quot; and Click **Create**.

      ![alt text](https://asfabricstorage.blob.core.windows.net/lab1images/3.png)

**Exercise 2: Create a new Azure API Management service instance**

1. In the  [Azure portal](https://portal.azure.com/), select  **Create a resource**  &gt;  **Enterprise Integration**  &gt;  **API management**.
2. In the  **API Management service**  window, enter settings. **Choose ** Create**.

      ![alt text](https://asfabricstorage.blob.core.windows.net/lab1images/4.png)

3. Once the API Management is deployed, Copy the Developer Portal URL (to be sued in Bot channel registration)

      ![alt text](https://asfabricstorage.blob.core.windows.net/lab1images/5.png)

**Exercise 3: Deploy Service Fabric Cluster**

1. Click Create a resource to add a new resource template.

      ![alt text](https://asfabricstorage.blob.core.windows.net/lab1images/6.png)

2. Search for the Service Fabric Cluster template in the Marketplace under Everything.

      ![alt text](https://asfabricstorage.blob.core.windows.net/lab1images/7.png)

3. Select Service Fabric Cluster from the list.

      ![alt text](https://asfabricstorage.blob.core.windows.net/lab1images/8.png)

4. Navigate to the Service Fabric Cluster blade, click Create,
5. The Create Service Fabric cluster blade has the following four steps:

**Task I: Basics** - In the Basics blade, you need to provide the basic details for your cluster.
* Enter the name of your cluster as &quot;onebank-fabric-cluster&quot;
* Enter a user name and password for Remote Desktop for the VMs.
* Make sure to select the Subscription that you want your cluster to be deployed to, especially if you have multiple subscriptions.
* Select the Resource Group created in first step.
* Select the region in which you want to create the cluster as &quot;West US 2&quot;

![alt text](https://asfabricstorage.blob.core.windows.net/lab1images/9.png)

**Task II: Cluster configuration**

![alt text](https://asfabricstorage.blob.core.windows.net/lab1images/10.png)

* Choose a name for your node type as &quot;Node 0&quot;
* The minimum size of VMs for the primary node type is driven by the durability tier you choose for the cluster. The default for the durability tier is bronze.
* Select the VM size as &quot;Standards\_D1\_v2&quot;
* Choose the number of VMs for the node type as 1
* Select Three node clusters
* Configure custom endpoints with 80,8770

![alt text](https://asfabricstorage.blob.core.windows.net/lab1images/11.png)

**Task III: Security**
* Select Basic in Security Configuration settings. To make setting up a secure test cluster easy for you, we have provided the  **Basic**  option.  Click on Key Vault for configuring required settings. Click on Create a new vault

![alt text](https://asfabricstorage.blob.core.windows.net/lab1images/12.png)

* Create a key Vault with given values and Click on Create

![alt text](https://asfabricstorage.blob.core.windows.net/lab1images/13.png)

* Now that the certificate is added to your key vault, you may see the following screen prompting you to edit the access policies for your Key vault. click on the Edit access **policies for.**  button.

![alt text](https://asfabricstorage.blob.core.windows.net/lab1images/14.png)

* Click on the advanced access policies and enable access to the Virtual Machines for deployment. It is recommended that you enable the template deployment as well. Once you have made your selections, do not forget to click the  **Save**  button and close out of the  **Access policies**  pane.

![alt text](https://asfabricstorage.blob.core.windows.net/lab1images/15.png)
      
![alt text](https://asfabricstorage.blob.core.windows.net/lab1images/16.png)

* You are now ready to proceed to the rest of the create cluster process.

**Task III: Summary**
* Now you are ready to deploy the cluster. Before you do that, download the certificate, look inside the large blue informational box for the link. Make sure to keep the cert in a safe place. you need it to connect to your cluster. Since the certificate you downloaded does not have a password, it is advised that you add one.
* To complete the cluster creation, click  **Create**. You can optionally download the template.

![alt text](https://asfabricstorage.blob.core.windows.net/lab1images/17.png)

6. You can see the creation progress in the notifications.

      ![alt text](https://asfabricstorage.blob.core.windows.net/lab1images/18.png)

7. Install the downloaded certificate from previous step.
8. Select Current User and Click Next

      ![alt text](https://asfabricstorage.blob.core.windows.net/lab1images/19.png)

9. Click Next. Leave Password Blank. Select Next

      ![alt text](https://asfabricstorage.blob.core.windows.net/lab1images/20.png)

10. Click on Next and Finish the Installation.

**Exercise 4: Configure Azure API Management service**

**Task I: Create and publish a product**

**a.** Click on Products in the menu on the left to display the Products page.

![alt text](https://asfabricstorage.blob.core.windows.net/lab1images/21.png)

**b.** Click  **+ Product**.

![alt text](https://asfabricstorage.blob.core.windows.net/lab1images/22.png)

**c.** When you add a product, supply the following information:
* Display name
* Name
* Description
* State as Published
* Requires subscription - Uncheck Require subscription checkbox
* Click Create to create the new product.

![alt text](https://asfabricstorage.blob.core.windows.net/lab1images/23.png)

**Task II: Add APIs to a product**

**a.** Select APIs from under API MANAGEMENT.

![alt text](https://asfabricstorage.blob.core.windows.net/lab1images/24.png)

**b.** Select  **Blank API**  from the list.

![alt text](https://asfabricstorage.blob.core.windows.net/lab1images/25.png)

**c.** Enter settings for the API.
* Display name
* Web Service URL – Fabric cluster end point. Update the port as 8770 and suffix with /api
* URL suffix as api
* Products – Select the Product created form previous step

**d.** Select Create.

![alt text](https://asfabricstorage.blob.core.windows.net/lab1images/26.png)

**Task III: Add the operation**

**a.** Select the API you created in the previous step.

**b.** Click + Add Operation.

![alt text](https://asfabricstorage.blob.core.windows.net/lab1images/27.png)
      
**c.** In the URL, select POST and enter &quot;/messages&quot; in the resource.

**d.** Enter &quot;Post /messages&quot; for Display name.

**e.** Select Save

![alt text](https://asfabricstorage.blob.core.windows.net/lab1images/28.png)

**Exercise 5: Create a Bot Channels Registration**

1. Click the New button found on the upper left-hand corner of the Azure portal, then select AI + Cognitive Services &gt; Bot Channels Registration.
2. A new blade will open with information about the Bot Channels Registration. Click the Create button to start the creation process.
3. In the Bot Service blade, provide the requested information about your bot as specified in the table below the image.

      ![alt text](https://asfabricstorage.blob.core.windows.net/lab1images/29.png)

4. Click  **Create**  to create the service and register your bot&#39;s messaging end point.
5. **Bot Channels Registration -** bot service does not have an app service associated with it. Because of that, this bot service only has a _MicrosoftAppID_. You need to generate the password manually and save it yourself.
* From the Settings blade, click Manage. This is the link appearing by the Microsoft App ID. This link will open a window where you can generate a new password.

![alt text](https://asfabricstorage.blob.core.windows.net/lab1images/30.png)

* Click  **Generate New Password**. This will generate a new password for your bot. Copy this password and save it to a file. This is the only time you will see this password. If you do not have the full password saved, you will need to repeat the process to create a new password should you need it later.

![alt text](https://asfabricstorage.blob.core.windows.net/lab1images/31.png)

* Click on Save at the end of the page. Close the page. In portal, Click on Save in Settings blade.

![alt text](https://asfabricstorage.blob.core.windows.net/lab1images/32.png)
