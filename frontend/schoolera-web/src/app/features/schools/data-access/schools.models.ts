export interface SchoolListItem {
  id: string;
  name: string;
  code: string;
  city?: string;
  isActive: boolean;
  createdAt?: string;
}

export interface CreateSchoolRequest {
  name: string;
  code: string;
  city?: string;
}
