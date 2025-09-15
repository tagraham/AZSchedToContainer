# Typescript Style Guide

# TypeScript Style Guide

## Naming Conventions

### ✅ DO
- Use `PascalCase` for types, interfaces, classes, and enums
  ```typescript
  interface UserProfile { }
  class DatabaseConnection { }
  type ApiResponse<T> = { }
  enum UserRole { Admin, User }
  ```

- Use `camelCase` for variables, functions, methods, and properties
  ```typescript
  const userName = 'john';
  function calculateTotal() { }
  const user = { firstName: 'John' };
  ```

- Use `SCREAMING_SNAKE_CASE` for module-level constants
  ```typescript
  const API_BASE_URL = 'https://api.example.com';
  const MAX_RETRY_ATTEMPTS = 3;
  ```

- Use descriptive names that clearly indicate purpose
  ```typescript
  const isUserAuthenticated = checkAuth();
  function validateEmailFormat(email: string): boolean { }
  ```

### ❌ DON'T
- Use `snake_case` for variables or functions
  ```typescript
  // Bad
  const user_name = 'john';
  function calculate_total() { }
  ```

- Use abbreviations or single letters (except for common cases like `i`, `e`)
  ```typescript
  // Bad
  const usr = getUser();
  function calc() { }
  ```

- Use Hungarian notation or type prefixes
  ```typescript
  // Bad
  const strUserName = 'john';
  interface IUser { }
  ```

## Type Definitions

### ✅ DO
- Prefer `interface` for object shapes and extensible contracts
  ```typescript
  interface User {
    id: string;
    name: string;
  }
  
  interface AdminUser extends User {
    permissions: string[];
  }
  ```

- Use `type` for unions, primitives, and computed types
  ```typescript
  type Status = 'loading' | 'success' | 'error';
  type UserKeys = keyof User;
  type EventHandler = (event: Event) => void;
  ```

- Define explicit return types for public functions
  ```typescript
  function processUser(user: User): Promise<ProcessedUser> {
    // implementation
  }
  ```

- Use utility types to derive new types
  ```typescript
  type CreateUser = Omit<User, 'id' | 'createdAt'>;
  type PartialUser = Partial<Pick<User, 'name' | 'email'>>;
  ```

- Use `readonly` for immutable properties
  ```typescript
  interface Config {
    readonly apiUrl: string;
    readonly features: readonly string[];
  }
  ```

### ❌ DON'T
- Use `any` type (use `unknown` instead when type is truly unknown)
  ```typescript
  // Bad
  function parse(data: any): any { }
  
  // Good
  function parse(data: unknown): ParsedData | null { }
  ```

- Use `type` for simple object shapes that might be extended
  ```typescript
  // Bad
  type User = {
    id: string;
    name: string;
  }
  ```

- Omit return types for complex functions
  ```typescript
  // Bad
  function complexCalculation(data) {
    // Complex logic that returns unclear type
  }
  ```

## Import/Export Organization

### ✅ DO
- Group imports in logical order with spacing
  ```typescript
  // 1. Node modules
  import React from 'react';
  import axios from 'axios';
  
  // 2. Internal modules
  import { UserService } from '@/services';
  import { Button } from '@/components';
  
  // 3. Types (consider separate import block)
  import type { User, ApiResponse } from '@/types';
  ```

- Use barrel exports (index.ts) for clean imports
  ```typescript
  // src/types/index.ts
  export type { User } from './user';
  export type { Product } from './product';
  
  // Usage
  import { User, Product } from '@/types';
  ```

- Use `type` imports for type-only imports
  ```typescript
  import type { ComponentProps } from 'react';
  import type { User } from './types';
  ```

### ❌ DON'T
- Mix import types with regular imports randomly
  ```typescript
  // Bad
  import { Button } from '@/components';
  import type { User } from '@/types';
  import { api } from '@/api';
  import type { Config } from '@/config';
  ```

- Use relative imports for distant modules
  ```typescript
  // Bad
  import { utils } from '../../../shared/utils';
  
  // Good
  import { utils } from '@/shared/utils';
  ```

## Error Handling & Type Safety

### ✅ DO
- Use type guards for runtime type checking
  ```typescript
  function isUser(obj: unknown): obj is User {
    return typeof obj === 'object' && obj !== null && 'id' in obj;
  }
  ```

- Handle null/undefined explicitly
  ```typescript
  function processUser(user: User | null): void {
    if (!user) {
      return;
    }
    // Process user safely
  }
  ```

- Use strict TypeScript configuration
  ```json
  {
    "compilerOptions": {
      "strict": true,
      "noImplicitAny": true,
      "strictNullChecks": true,
      "noImplicitReturns": true,
      "noUnusedLocals": true,
      "noUnusedParameters": true
    }
  }
  ```

### ❌ DON'T
- Use non-null assertion (`!`) unless absolutely necessary
  ```typescript
  // Bad (unless you're 100% certain)
  const user = getUser()!;
  ```

- Ignore TypeScript errors with `@ts-ignore`
  ```typescript
  // Bad
  // @ts-ignore
  const result = someUntypedFunction();
  ```

- Use `as any` to bypass type checking
  ```typescript
  // Bad
  const data = response as any;
  ```

## Code Documentation

### ✅ DO
- Document complex types and interfaces
  ```typescript
  /**
   * Represents a paginated API response
   * @template T The type of items in the data array
   */
  interface PaginatedResponse<T> {
    data: T[];
    /** Current page number (1-indexed) */
    page: number;
    /** Total number of pages */
    totalPages: number;
  }
  ```

- Use JSDoc for public APIs
  ```typescript
  /**
   * Calculates the total price including tax
   * @param price - The base price
   * @param taxRate - Tax rate as decimal (e.g., 0.08 for 8%)
   * @returns The total price with tax applied
   */
  function calculateTotal(price: number, taxRate: number): number {
    return price * (1 + taxRate);
  }
  ```

### ❌ DON'T
- Over-document obvious code
  ```typescript
  // Bad
  /** Gets the user name */
  function getUserName(): string {
    return this.name;
  }
  ```

- Use comments to explain what code does (code should be self-explanatory)
  ```typescript
  // Bad
  // Loop through users and increment counter
  for (const user of users) {
    count++;
  }
  ```

## General Best Practices

### ✅ DO
- Enable and use ESLint with TypeScript rules
- Use Prettier for consistent code formatting
- Prefer composition over inheritance
- Keep functions small and focused on single responsibility
- Use meaningful variable names that don't require comments

### ❌ DON'T
- Disable TypeScript strict checks without good reason
- Mix tabs and spaces (use Prettier to avoid this)
- Create overly complex type definitions that are hard to understand
- Use deep nesting when early returns or extraction can improve readability