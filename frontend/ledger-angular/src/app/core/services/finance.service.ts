import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { TransferDto } from '../models/transfer.models';
import { WalletDto, PagedResponse } from '../models/wallet.models';

@Injectable({ providedIn: 'root' })
export class FinanceService {
  private readonly base = `${environment.apiUrl}/finance`;

  constructor(private http: HttpClient) {}

  getAllTransfers(page = 1, pageSize = 20): Observable<PagedResponse<TransferDto>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<PagedResponse<TransferDto>>(`${this.base}/transfers`, { params });
  }

  getTransfer(id: string): Observable<TransferDto> {
    return this.http.get<TransferDto>(`${this.base}/transfers/${id}`);
  }

  getAllWallets(page = 1, pageSize = 20): Observable<PagedResponse<WalletDto>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<PagedResponse<WalletDto>>(`${this.base}/wallets`, { params });
  }
}
