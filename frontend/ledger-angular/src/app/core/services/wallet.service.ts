import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { WalletDto, WalletDetailDto, CreateWalletRequest, LedgerEntryDto, PagedResponse } from '../models/wallet.models';

@Injectable({ providedIn: 'root' })
export class WalletService {
  private readonly base = `${environment.apiUrl}/wallets`;

  constructor(private http: HttpClient) {}

  getWallets(): Observable<WalletDto[]> {
    return this.http.get<WalletDto[]>(this.base);
  }

  getWallet(id: string): Observable<WalletDetailDto> {
    return this.http.get<WalletDetailDto>(`${this.base}/${id}`);
  }

  createWallet(req: CreateWalletRequest): Observable<WalletDto> {
    return this.http.post<WalletDto>(this.base, req);
  }

  getHistory(id: string, page = 1, pageSize = 20): Observable<PagedResponse<LedgerEntryDto>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<PagedResponse<LedgerEntryDto>>(`${this.base}/${id}/history`, { params });
  }
}
