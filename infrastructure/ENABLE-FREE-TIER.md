# Enable Cosmos DB Free Tier

This guide shows you how to enable the Cosmos DB free tier and reduce your monthly costs from **$25/month to $0.60/month**.

## What You Get with Free Tier

- **1,000 RU/s** of provisioned throughput (free)
- **25 GB** of storage (free)
- Perfect for personal projects and resumes
- **One per Azure subscription** (limit)

## Current Setup

Your resume uses:
- **400 RU/s** throughput (well within 1000 limit)
- **< 1 GB** storage (well within 25 GB limit)

✅ **You're eligible for 100% free Cosmos DB!**

---

## Option 1: Enable on Existing Account (Recommended)

⚠️ **Important**: You can only have ONE free tier Cosmos DB per subscription. If you already have another free tier Cosmos DB, skip to Option 2.

### Step 1: Check Eligibility

1. Go to **Azure Portal**: https://portal.azure.com
2. Search for **Subscriptions**
3. Click your subscription
4. In left menu, click **Resource providers**
5. Search for **Microsoft.DocumentDB**
6. Check if you have any other Cosmos DB accounts with free tier enabled

### Step 2: Enable Free Tier on Existing Account

**Via Azure Portal:**

1. Go to your Cosmos DB account (search for your account name)
2. Unfortunately, **free tier cannot be enabled after account creation** via the portal
3. You must use Azure CLI (see below)

**Via Azure CLI:**

```bash
# Login to Azure
az login

# Get your Cosmos DB account name
az cosmosdb list --resource-group azureresume-rg --query "[].name" -o tsv

# Enable free tier (replace <account-name> with actual name)
az cosmosdb update \
  --name <account-name> \
  --resource-group azureresume-rg \
  --enable-free-tier true
```

**Expected Output:**
```json
{
  "enableFreeTier": true,
  ...
}
```

### Step 3: Verify Free Tier is Enabled

1. Go to **Azure Portal** → Your Cosmos DB account
2. Click **Settings** → **Features**
3. Look for **"Free Tier Discount Applied"** badge
4. Or check the **Overview** page for the free tier banner

---

## Option 2: Recreate Cosmos DB with Free Tier (If Option 1 Fails)

If you can't enable free tier on existing account, you need to recreate it.

### ⚠️ Before You Start - Backup Data

Your Counter document:
```json
{
  "id": "index",
  "count": <current-count>
}
```

**Save this number!** You'll need to restore it after recreating.

### Step 1: Note Current Settings

1. Go to **Cosmos DB account** → **Keys**
2. **Copy the connection string** (you'll need to update Function App later)

### Step 2: Delete Old Cosmos DB Account

```bash
# Delete the old account
az cosmosdb delete \
  --name <old-account-name> \
  --resource-group azureresume-rg \
  --yes
```

Wait for deletion to complete (~5 minutes).

### Step 3: Deploy New Account with Free Tier

```bash
# Deploy the updated Bicep template
cd infrastructure

az deployment group create \
  --resource-group azureresume-rg \
  --template-file main.bicep \
  --parameters environment=prod
```

This creates:
- ✅ New Cosmos DB account with free tier enabled
- ✅ CloudResume database
- ✅ Counter container
- ✅ Messages container

### Step 4: Restore Counter Data

1. Go to **Azure Portal** → New Cosmos DB account
2. Click **Data Explorer**
3. Expand **CloudResume** → **Counter**
4. Click **New Item**
5. Paste this JSON (replace `<your-count>` with saved number):

```json
{
  "id": "index",
  "count": <your-count>
}
```

6. Click **Save**

### Step 5: Update Function App Connection String

1. Get the new connection string:

```bash
az cosmosdb keys list \
  --name <new-account-name> \
  --resource-group azureresume-rg \
  --type connection-strings \
  --query "connectionStrings[0].connectionString" -o tsv
```

2. Update Function App:

```bash
az functionapp config appsettings set \
  --name resumefunctionapp-win \
  --resource-group azureresume-rg \
  --settings "CloudResume=<new-connection-string>"
```

3. Restart Function App:

```bash
az functionapp restart \
  --name resumefunctionapp-win \
  --resource-group azureresume-rg
```

### Step 6: Test

1. Visit: https://resumefunctionapp-win-cqczeqc6d5gtdfbb.australiaeast-01.azurewebsites.net/api/getResumeFunction
2. Should see your restored count
3. Visit your website - counter should work!

---

## Option 3: Use Bicep Template for Clean Deployment

If you want to start fresh with everything properly configured:

```bash
# Delete entire resource group (⚠️ WARNING: Deletes everything!)
az group delete --name azureresume-rg --yes

# Recreate resource group
az group create --name azureresume-rg --location australiaeast

# Deploy everything with free tier
cd infrastructure
az deployment group create \
  --resource-group azureresume-rg \
  --template-file main.bicep \
  --parameters environment=prod
```

Then redeploy your code using GitHub Actions.

---

## Verify Free Tier is Working

### Check in Azure Portal

1. Go to **Cosmos DB account** → **Overview**
2. Look for green banner: **"Free Tier Discount Applied"**
3. Or go to **Metrics** → Should show 1000 RU/s available

### Check Your Bill

1. Go to **Cost Management + Billing**
2. Navigate to your subscription
3. Check **Cost Analysis**
4. Filter by **Cosmos DB**
5. Should show **$0** for provisioned throughput

---

## Monthly Cost Breakdown (After Free Tier)

| Service | Before | After | Savings |
|---------|--------|-------|---------|
| Cosmos DB | $24/mo | **$0/mo** | $24/mo |
| Functions | $0/mo | **$0/mo** | $0 |
| Storage | $0.50/mo | **$0.50/mo** | $0 |
| CDN | $0.10/mo | **$0.10/mo** | $0 |
| App Insights | $0/mo | **$0/mo** | $0 |
| **TOTAL** | **$24.60/mo** | **$0.60/mo** | **$24/mo** |

**Annual Savings: $288** 🎉

---

## Limitations

- ✅ **1000 RU/s throughput** (your resume uses ~400 RU/s)
- ✅ **25 GB storage** (your resume uses < 1 GB)
- ⚠️ **One account per subscription** (can't use for other projects)
- ⚠️ **Single region only** (multi-region costs extra)

For your resume: **No limitations!** You're well within free tier limits.

---

## Troubleshooting

### "Free tier already in use"

- You have another Cosmos DB with free tier enabled
- Delete it or use Option 2 above

### "Cannot enable free tier on existing account"

- Free tier must be enabled at creation time
- Use Option 2 to recreate the account

### "Function App can't connect to Cosmos DB"

- Update the connection string in Function App settings
- Restart the Function App

---

## Need Help?

If you run into issues:

1. Check Azure Portal notifications for error details
2. Verify you only have one Cosmos DB account
3. Ensure the connection string is updated in Function App
4. Test the health endpoint: `/api/health`

---

**Last Updated**: January 8, 2026
