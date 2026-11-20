# Interview Guide

## 1. Explain the application

I built a Property Showing Booking API using .NET 8, ASP.NET Core Minimal APIs, EF Core and SQLite. Owners can publish showing slots, renters can reserve seats, and the API enforces capacity and booking lifecycle rules. I separated HTTP contracts, domain entities, persistence and business use cases so the solution remains easy to understand and extend.

## 2. How do you prevent overbooking?

I don't depend on reading the current seat count and then checking it in C#, because two requests could read the same remaining capacity. Instead, I execute a conditional database update that increments booked seats only when the new total stays within capacity. The update runs in a transaction with booking creation, so a failed booking rolls back the reservation.

## 3. Why keep BookedSeats?

It makes the capacity reservation an atomic database operation. The value is maintained in the same transaction as booking creation and cancellation. A database check constraint protects its valid range. An alternative is calculating the sum of Confirmed bookings, but that requires a more complex concurrency-safe reservation strategy.

## 4. Why a filtered unique index?

Application validation alone can race. A filtered unique index on slot and normalized renter email, for Confirmed rows only, makes the one-active-booking rule a database invariant. Once a booking is Cancelled, a new active booking can be created.

## 5. Why SQLite?

The assignment allows SQLite, SQL Server in Docker or EF Core InMemory. SQLite provides durable relational storage with minimal setup for a reviewer. It also supports transactions, constraints and indexes. For production scale or multiple application instances, I would evaluate SQL Server/PostgreSQL and test the exact isolation/locking behavior under load.

## 6. What would you improve?

I would add authentication and authorization, deriving the actor from the authenticated user rather than accepting it from the request. I would add EF migrations, centralized Problem Details error handling, integration and concurrency tests, structured logging, idempotency and production database load testing.

## 7. Be transparent

Before the interview, run the solution yourself, understand every transaction and SQL statement, verify all endpoints, and update the implementation log with actual results. Do not claim tests or concurrency scenarios passed until you have executed them.
