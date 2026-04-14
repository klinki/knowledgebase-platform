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
      { path: 'search', loadComponent: () => import('./features/search/search.component').then(m => m.SearchComponent) },
      { path: 'assistant', loadComponent: () => import('./features/assistant/assistant.component').then(m => m.AssistantComponent) },
      { path: 'captures/new', loadComponent: () => import('./features/captures/create-capture.component').then(m => m.CreateCaptureComponent) },
      { path: 'captures', loadComponent: () => import('./features/captures/captures.component').then(m => m.CapturesComponent) },
      { path: 'captures/:id', loadComponent: () => import('./features/captures/capture-detail.component').then(m => m.CaptureDetailComponent) },
      { path: 'topics', loadComponent: () => import('./features/topics/topics.component').then(m => m.TopicsComponent) },
      { path: 'topics/:id', loadComponent: () => import('./features/topics/topic-detail.component').then(m => m.TopicDetailComponent) },
      { path: 'labels', loadComponent: () => import('./features/labels/labels.component').then(m => m.LabelsComponent) },
      { path: 'settings', loadComponent: () => import('./features/settings/settings.component').then(m => m.SettingsComponent) },
      { path: 'tags', loadComponent: () => import('./features/tags/tags.component').then(m => m.TagsComponent) },
      { path: 'admin/invitations', loadComponent: () => import('./features/admin/invitations.component').then(m => m.InvitationsComponent) },
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: '**', loadComponent: () => import('./features/shell/shell-not-found.component').then(m => m.ShellNotFoundComponent) }
    ]
  }
];
