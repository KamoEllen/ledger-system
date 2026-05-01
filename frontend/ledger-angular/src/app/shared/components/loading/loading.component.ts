import { Component } from '@angular/core';

@Component({
  selector: 'app-loading',
  standalone: true,
  template: `
    <div class="loading-wrapper">
      <div class="spinner"></div>
    </div>
  `,
  styles: [`
    .loading-wrapper {
      display: flex;
      justify-content: center;
      align-items: center;
      padding: 48px;
    }
    .spinner {
      width: 40px;
      height: 40px;
      border: 3px solid rgba(0,230,118,0.15);
      border-top-color: #00e676;
      border-radius: 50%;
      animation: spin 0.8s linear infinite;
    }
    @keyframes spin { to { transform: rotate(360deg); } }
  `]
})
export class LoadingComponent {}
