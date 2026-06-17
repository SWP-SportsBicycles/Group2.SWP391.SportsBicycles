# SBE - Used Sports Bicycle Exchange Platform

SBE is an online marketplace for buying and selling used sports bicycles.  
The system helps buyers purchase inspected bicycles safely and allows sellers to list their bicycles through an online inspection process.

## Main Features

- User authentication and role-based access control
- Roles: Guest, Buyer, Seller, Inspector, Admin
- Create and manage used bicycle listings
- Upload bicycle images and videos for inspection
- Online inspection workflow for listed bicycles
- Search and browse bicycle listings
- 5-minute listing lock when a buyer starts checkout
- Online payment integration with PayOS
- Shipping fee calculation and delivery tracking with GHN
- Escrow flow to hold payment until delivery is completed
- Refund and cancellation handling
- Admin management for users, listings, orders, fees, and refunds

## Tech Stack

- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- ReactJS / NextJS
- PayOS
- GHN

## Project Structure

```bash
Group2.SWP391.SportsBicycles/
├── Group2.SWP391.SportsBicycles.API
├── Group2.SWP391.SportsBicycles.Service
├── Group2.SWP391.SportsBicycles.DAL
├── Group2.SWP391.SportsBicycles.Common
└── Group2.SWP391.SportsBicycles.sln
