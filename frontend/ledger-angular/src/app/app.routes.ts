import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { financeGuard, adminGuard } from './core/guards/role.guard';

export const routes: Routes = [
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },

  {
    path: 'auth',
    children: [
      { path: 'login', loadComponent: () => import('./features/auth/login/login.component').then(m => m.LoginComponent) },
      { path: 'register', loadComponent: () => import('./features/auth/register/register.component').then(m => m.RegisterComponent) },
      { path: '', redirectTo: 'login', pathMatch: 'full' }
    ]
  },

  {
    path: 'dashboard',
    canActivate: [authGuard],
    loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent)
  },

  {
    path: 'wallets',
    canActivate: [authGuard],
    children: [
      { path: '', loadComponent: () => import('./features/wallets/wallet-list/wallet-list.component').then(m => m.WalletListComponent) },
      { path: 'create', loadComponent: () => import('./features/wallets/wallet-create/wallet-create.component').then(m => m.WalletCreateComponent) },
      { path: ':id', loadComponent: () => import('./features/wallets/wallet-detail/wallet-detail.component').then(m => m.WalletDetailComponent) }
    ]
  },

  {
    path: 'transfers',
    canActivate: [authGuard],
    children: [
      { path: '', loadComponent: () => import('./features/transfers/transfer-list/transfer-list.component').then(m => m.TransferListComponent) },
      { path: 'create', loadComponent: () => import('./features/transfers/transfer-create/transfer-create.component').then(m => m.TransferCreateComponent) },
      { path: ':id', loadComponent: () => import('./features/transfers/transfer-detail/transfer-detail.component').then(m => m.TransferDetailComponent) }
    ]
  },

  {
    path: 'finance',
    canActivate: [authGuard, financeGuard],
    children: [
      { path: 'transfers', loadComponent: () => import('./features/finance/finance-transfers/finance-transfers.component').then(m => m.FinanceTransfersComponent) },
      { path: 'wallets', loadComponent: () => import('./features/finance/finance-wallets/finance-wallets.component').then(m => m.FinanceWalletsComponent) },
      { path: '', redirectTo: 'transfers', pathMatch: 'full' }
    ]
  },

  {
    path: 'admin',
    canActivate: [authGuard, adminGuard],
    children: [
      { path: 'users', loadComponent: () => import('./features/admin/admin-users/admin-users.component').then(m => m.AdminUsersComponent) },
      { path: '', redirectTo: 'users', pathMatch: 'full' }
    ]
  },

  { path: '**', redirectTo: '/dashboard' }
];
