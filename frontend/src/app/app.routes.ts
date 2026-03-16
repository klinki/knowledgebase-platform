import { Routes } from '@angular/router';
import { LoginComponent } from './features/auth/login.component';
import { AcceptInvitationComponent } from './features/auth/accept-invitation.component';
import { ShellComponent } from './features/shell/shell.component';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'accept-invite', component: AcceptInvitationComponent },
  {
    path: '',
    component: ShellComponent,
    canActivate: [authGuard],
    children: [
      { path: 'dashboard', loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent) },
      { path: 'tags', loadComponent: () => import('./features/tags/tags.component').then(m => m.TagsComponent) },
      { path: 'admin/invitations', loadComponent: () => import('./features/admin/invitations.component').then(m => m.InvitationsComponent) },
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' }
    ]
  }
];
