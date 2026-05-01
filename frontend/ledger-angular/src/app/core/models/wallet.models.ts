export interface WalletDto {
  id: string;
  userId: string;
  currency: string;
  balance: number;
  isActive: boolean;
  createdAt: string;
}

export interface WalletDetailDto extends WalletDto {}

export interface CreateWalletRequest {
  currency: string;
}

export interface LedgerEntryDto {
  id: string;
  walletId: string;
  transferId: string | null;
  entryType: 'Debit' | 'Credit';
  amount: number;
  balanceAfter: number;
  description: string | null;
  createdAt: string;
}

export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}
