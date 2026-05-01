import { Component, computed, signal } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, CommonModule],
  template: `
    <aside class="sidebar" [class.collapsed]="collapsed()">
      <div class="sidebar-header">
        <div class="logo" *ngIf="!collapsed()">
          <span class="logo-icon">⬡</span>
          <span class="logo-text">LedgerFlow</span>
        </div>
        <button class="collapse-btn" (click)="toggle()">
          <span>{{ collapsed() ? '→' : '←' }}</span>
        </button>
      </div>

      <nav class="sidebar-nav">
        <a routerLink="/dashboard" routerLinkActive="active" class="nav-item">
          <span class="nav-icon">⊞</span>
          <span class="nav-label" *ngIf="!collapsed()">Dashboard</span>
        </a>
        <a routerLink="/wallets" routerLinkActive="active" class="nav-item">
          <span class="nav-icon">◈</span>
          <span class="nav-label" *ngIf="!collapsed()">Wallets</span>
        </a>
        <a routerLink="/transfers" routerLinkActive="active" class="nav-item">
          <span class="nav-icon">⇄</span>
          <span class="nav-label" *ngIf="!collapsed()">Transfers</span>
        </a>

        <div class="nav-divider" *ngIf="!collapsed() && isFinanceOrAdmin()">
          <span>Finance</span>
        </div>
        <div class="nav-divider-line" *ngIf="collapsed() && isFinanceOrAdmin()"></div>

        <ng-container *ngIf="isFinanceOrAdmin()">
          <a routerLink="/finance/transfers" routerLinkActive="active" class="nav-item">
            <span class="nav-icon">◉</span>
            <span class="nav-label" *ngIf="!collapsed()">All Transfers</span>
          </a>
          <a routerLink="/finance/wallets" routerLinkActive="active" class="nav-item">
            <span class="nav-icon">◈</span>
            <span class="nav-label" *ngIf="!collapsed()">All Wallets</span>
          </a>
        </ng-container>

        <div class="nav-divider" *ngIf="!collapsed() && isAdmin()">
          <span>Admin</span>
        </div>
        <div class="nav-divider-line" *ngIf="collapsed() && isAdmin()"></div>

        <ng-container *ngIf="isAdmin()">
          <a routerLink="/admin/users" routerLinkActive="active" class="nav-item">
            <span class="nav-icon">◎</span>
            <span class="nav-label" *ngIf="!collapsed()">Users</span>
          </a>
        </ng-container>
      </nav>

      <div class="sidebar-footer" *ngIf="!collapsed()">
        <div class="user-info">
          <div class="user-avatar">{{ userInitial() }}</div>
          <div class="user-details">
            <span class="user-email">{{ userEmail() }}</span>
            <span class="user-role badge" [class]="roleBadgeClass()">{{ userRole() }}</span>
          </div>
        </div>
        <button class="logout-btn" (click)="logout()">Logout</button>
      </div>
    </aside>
  `,
  styleUrls: ['./sidebar.component.scss']
})
export class SidebarComponent {
  collapsed = signal(false);

  constructor(private auth: AuthService) {}

  isAdmin = this.auth.isAdmin;
  isFinanceOrAdmin = this.auth.isFinanceOrAdmin;

  userEmail = computed(() => this.auth.user()?.email ?? '');
  userRole = computed(() => this.auth.user()?.role ?? '');
  userInitial = computed(() => (this.auth.user()?.email ?? 'U')[0].toUpperCase());
  roleBadgeClass = computed(() => {
    const role = this.auth.user()?.role;
    if (role === 'Admin') return 'badge-red';
    if (role === 'Finance') return 'badge-yellow';
    return 'badge-green';
  });

  toggle(): void {
    this.collapsed.update(v => !v);
  }

  logout(): void {
    this.auth.logout();
  }
}
