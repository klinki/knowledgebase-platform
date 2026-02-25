---
description: React and TypeScript Development Conventions for Inviser.FrontendReact
---

# React / TypeScript Guidelines

These are the conventions that should be followed when working within the frontend `Inviser.FrontendReact` project.

## Naming Conventions
- **Components**: `PascalCase` (`UserList.tsx`, `UserListComponent` etc.)
- **Hooks**: `camelCase` starting with `use` (`useUser`, `useAuth`)
- **Constants**: `PascalCase` for exported configuration variables, `camelCase` for internal variables.
- **Files**: `kebab-case` or `PascalCase` for React components.

## TypeScript Configuration
- **Target**: ES2021
- **Strict mode**: enabled
- **JSX**: preserve mode with Preact `h` function

```json
{
  "compilerOptions": {
    "target": "ES2021",
    "strict": true,
    "jsx": "preserve",
    "jsxFactory": "h",
    "jsxFragmentFactory": "Fragment"
  }
}
```

## React Patterns
- Default to Functional Components using React Hooks.
- Always use explicit **TypeScript interfaces** or types for props.
- Use React Context for scoped global state overrides, but avoid prop-drilling if not required.

```tsx
interface UserListProps {
  users: User[];
  onSelect: (user: User) => void;
}

export const UserList: React.FC<UserListProps> = ({ users, onSelect }) => {
  return (
    <ul>
      {users.map(user => (
        <li key={user.id} onClick={() => onSelect(user)}>
          {user.name}
        </li>
      ))}
    </ul>
  );
};
```

## Import Structures
- Group imports organically: `React/library tools` > `Third-party vendor modules` > `Internal mapped files (@)` > `Relative imports (./)`.
- Named exports are highly preferred over default exports! 

```tsx
import { useState, useEffect } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { Button } from '@coreui/react';
import { UserCard } from '@/components/UserCard';
import { selectCurrentUser } from '@/store/authSlice';
```

## State Management Architecture
- **Global**: Redux Toolkit handles heavy global/authenticated states.
- **Component**: Local React state variables.
- **High Performance**: Utilize Preact signals for reactivity streams that should not trigger heavy React re-renders.
