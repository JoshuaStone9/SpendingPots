# Spending Pots Project

A small C# console app that helps you manage “spending pots” (budget categories) and optionally syncs them with real(ish) bank account balances using the Plaid **sandbox** API.

## Table of Contents

1. [Overview](#overview)  
2. [High-Level Architecture](#high-level-architecture)  
3. [Data Model](#data-model)  
4. [Control Flow](#control-flow)  
5. [Plaid API Integration](#plaid-api-integration)  
6. [Local Persistence (`spendingpots.txt`)](#local-persistence-spendingpotstxt)  
7. [Configuration & Environment](#configuration--environment)  
8. [How to Run](#how-to-run)  
9. [Potential Improvements](#potential-improvements)

## Overview

This project is a **console-based budget tool** with the concept of “spending pots” (e.g. Food, Entertainment, Savings). Each pot has a name and a balance.

You can:

- View current pot balances  
- Add money to an existing pot  
- Add a new pot  
- Sync pots with **Plaid sandbox bank accounts** (pulling in balances as if they were real accounts)

Data is stored in a simple text file (`spendingpots.txt`) in the same folder as the executable, so your pots persist between runs.

## High-Level Architecture

Everything lives inside a single class: `Program` in the `spendingPotsProject` namespace.

You can think of it roughly in layers:

1. **Presentation / UI Layer (Console)**  
   Handles user interaction, output, and menus.

2. **Domain Logic (Spending Pots)**  
   Logic for adding pots, updating balances, and displaying data.

3. **Persistence Layer (File Storage)**  
   Reads and writes the text file that stores pot data locally.

4. **External Integration Layer (Plaid API)**  
   Uses the Plaid sandbox service to fetch fake bank account balances.

All state is kept in two static lists:

```csharp
List<string> spendingpots;
List<int> potBalances;
```

These must always remain aligned by index.

## Data Model

The app uses simple in-memory lists:

### Pots
A list of strings representing category names.

### Balances
A list of integers representing whole-dollar values assigned to pots.

Example:

```
spendingpots[0] = "Food"
potBalances[0]  = 250
```

Meaning: Food → $250.

## Control Flow

### Program Startup

1. Prints welcome text.  
2. Loads pot data using `LoadBalancesFromFile()`.  
3. Shows the main menu.  
4. Enters `MenuOptionsAsync()` to wait for user actions.

### Menu Options

- **1. View Balances**  
- **2. Add to Pot**  
- **3. Add New Pot**  
- **4. Sync from Plaid (sandbox)**  
- **5. Exit**

## Plaid API Integration

Plaid is used here in **sandbox mode** which means:

- No real bank login is required  
- A fake “public token” and fake accounts are generated  
- The workflow imitates real banking connections  

### CreatePlaidClient()

- Loads environment variables  
- Retrieves `PLAID_CLIENT_ID` and `PLAID_SECRET`  
- Constructs a Plaid client targeting the sandbox environment  

### SyncFromPlaidSandboxAsync()

1. Copies custom pots  
2. Requests a Plaid sandbox public token  
3. Exchanges it for an access token  
4. Fetches account balances  
5. Replaces pot list with Plaid accounts  
6. Re-appends custom pots  
7. Saves merged list to disk  

## Local Persistence (`spendingpots.txt`)

Pots are stored as:

```
Name: $Amount
```

File is rewritten completely on each save.

## Configuration & Environment

Create a `.env` file:

```
PLAID_CLIENT_ID=your_client_id
PLAID_SECRET=your_secret
```

## How to Run

```
dotnet add package Going.Plaid
dotnet add package DotNetEnv
dotnet run
```

## Potential Improvements

- Use decimals instead of ints  
- Add transfers between pots  
- Add UI or web API  
- Add reporting or graphs  
