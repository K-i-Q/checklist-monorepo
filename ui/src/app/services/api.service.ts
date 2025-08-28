import { Injectable } from '@angular/core';
import { HttpClient, HttpResponse } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Vehicle {
  id: string;
  plate: string;
  model: string;
}
export interface Template {
  id: string;
  name: string;
}
export interface TemplateItem {
  id: string;
  label: string;
  order: number;
  required: boolean;
}

export interface ExecutionItem {
  id: string;
  executionId: string;
  templateItemId: string;
  status: number;
  rowVersion: string;
  observation?: string | null;
}
export interface Execution {
  id: string;
  templateId: string;
  vehicleId: string;
  executorId?: string | null;
  startedAt?: string | null;
  lockedAt?: string | null;
  status: number;
  referenceDate?: string | null;
  rowVersion: string;
  items: ExecutionItem[];
}

@Injectable({ providedIn: 'root' })
export class ApiService {
  private base = 'http://localhost:5095/api/checklists';

  constructor(private http: HttpClient) {}

  getVehicles(): Observable<Vehicle[]> {
    return this.http.get<Vehicle[]>(`${this.base}/vehicles`);
  }
  getTemplates(): Observable<Template[]> {
    return this.http.get<Template[]>(`${this.base}/templates`);
  }
  getTemplateItems(templateId: string): Observable<TemplateItem[]> {
    return this.http.get<TemplateItem[]>(`${this.base}/templates/${templateId}/items`);
  }

  createExecution(payload: {
    templateId: string;
    vehicleId: string;
    referenceDate?: string;
  }): Observable<HttpResponse<any>> {
    return this.http.post(`${this.base}/executions`, payload, { observe: 'response' });
  }

  getExecution(id: string): Observable<Execution> {
    return this.http.get<Execution>(`${this.base}/executions/${id}`);
  }

  startExecution(id: string, executorId: string) {
    return this.http.post(`${this.base}/executions/${id}/start`, { executorId });
  }

  patchItem(
    executionId: string,
    templateItemId: string,
    body: { status: number; observation?: string | null; rowVersion: string },
    _userId?: string
  ) {
    return this.http.patch(`${this.base}/executions/${executionId}/items/${templateItemId}`, body);
  }

  submitExecution(id: string, rowVersion: string, _userId?: string) {
    return this.http.post(`${this.base}/executions/${id}/submit`, { rowVersion });
  }

  approveExecution(
    id: string,
    decision: 0 | 1,
    notes: string | null,
    rowVersion: string,
    _supervisorId?: string
  ) {
    return this.http.post(`${this.base}/executions/${id}/approve`, {
      decision,
      notes,
      rowVersion,
    });
  }
}
