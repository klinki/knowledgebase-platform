import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';

import { DashboardStateService } from '../../core/services/dashboard-state.service';
import { AdminProcessingStateService } from '../../core/services/admin-processing-state.service';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  dashboardState = inject(DashboardStateService);
  adminProcessingState = inject(AdminProcessingStateService);
  authService = inject(AuthService);

  isAdmin = computed(() => this.authService.currentUser()?.role === 'admin');
  items = computed(() => this.dashboardState.recentCaptures());
  loading = computed(() => this.dashboardState.loading());
  error = computed(() => this.dashboardState.error());

  async ngOnInit(): Promise<void> {
    await this.dashboardState.loadOverview();
    if (this.isAdmin()) {
      await this.adminProcessingState.loadOverview();
    }
  }

  async toggleProcessing(): Promise<void> {
    if (!this.isAdmin()) {
      return;
    }

    if (this.adminProcessingState.isPaused()) {
      await this.adminProcessingState.resumeProcessing();
      return;
    }

    await this.adminProcessingState.pauseProcessing();
  }
}
