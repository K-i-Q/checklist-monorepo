import { Component, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService, Execution, Vehicle, Template, TemplateItem } from '../../services/api.service';
import { ToastService } from '../../ui/toast.service';
import { ToastsComponent } from '../../ui/toasts.component';
import { ThemeToggleComponent } from '../../ui/theme-toggle.component';

@Component({
  selector: 'app-execution',
  standalone: true,
  imports: [CommonModule, FormsModule, ToastsComponent, ThemeToggleComponent],
  templateUrl: './execution.component.html',
})
export class ExecutionComponent implements OnInit {
  vehicles = signal<Vehicle[]>([]);
  templates = signal<Template[]>([]);
  templateItems = signal<TemplateItem[]>([]);

  templateId = '';
  vehicleId = '';
  referenceDate = '';

  executorId = '11111111-1111-1111-1111-111111111111';
  supervisorId = '22222222-2222-2222-2222-222222222222';

  exec = signal<Execution | null>(null);

  busy = signal(false);
  creating = signal(false);
  loadingExec = signal(false);
  starting = signal(false);
  submitting = signal(false);
  approving = signal<0 | 1 | null>(null);
  patchingId = signal<string | null>(null);

  error = signal<string | null>(null);

  constructor(private api: ApiService, private toast: ToastService) {}

  ngOnInit() {
    this.loadLists();
  }

  private loadLists() {
    this.busy.set(true);
    this.api.getVehicles().subscribe({
      next: (v) => {
        this.vehicles.set(v);
        if (!this.vehicleId && v.length) this.vehicleId = v[0].id;
        this.busy.set(false);
      },
      error: (e) => {
        this.error.set(e?.message ?? 'Falha ao carregar veículos');
        this.busy.set(false);
      },
    });
    this.busy.set(true);
    this.api.getTemplates().subscribe({
      next: (t) => {
        this.templates.set(t);
        if (!this.templateId && t.length) this.templateId = t[0].id;
        this.busy.set(false);
      },
      error: (e) => {
        this.error.set(e?.message ?? 'Falha ao carregar templates');
        this.busy.set(false);
      },
    });
  }

  private loadTemplateItems(templateId: string) {
    this.api.getTemplateItems(templateId).subscribe({
      next: (items) => this.templateItems.set(items),
      error: (e) => this.error.set(e?.message ?? 'Falha ao listar itens do template'),
    });
  }

  private msgFromErr(e: any, fallback = 'Ocorreu um erro') {
    if (!e) return fallback;
    if (typeof e === 'string') return e;
    const he = e as { error?: any; message?: string; status?: number };
    if (typeof he?.error === 'string') return he.error;

    if (he?.error && typeof he.error === 'object') {
      if (typeof he.error.detail === 'string') return he.error.detail;
      if (typeof he.error.title === 'string') return he.error.title;
      if (typeof he.error.message === 'string') return he.error.message;
    }

    if (typeof he?.message === 'string') return he.message;
    return fallback;
  }

  private shouldReload(e: any): boolean {
    const code = e?.status ?? e?.statusCode;
    if ([400, 403, 409, 412].includes(code)) return true;

    const txt =
      (typeof e?.error === 'string' ? e.error : '') +
      ' ' +
      (typeof e?.message === 'string' ? e.message : '');
    return /vers[aã]o|rowversion|submitted|aprovad/i.test(txt);
  }

  private idFromLocation(loc?: string | null): string | null {
    if (!loc) return null;
    const m = loc.match(/executions\/([0-9a-fA-F-]{36})/);
    return m?.[1] ?? null;
  }

  create() {
    if (!this.templateId || !this.vehicleId) {
      this.error.set('Selecione o Template e o Veículo');
      return;
    }

    this.creating.set(true);
    this.error.set(null);

    this.api
      .createExecution({
        templateId: this.templateId,
        vehicleId: this.vehicleId,
        referenceDate: this.referenceDate || undefined,
      })
      .subscribe({
        next: (resp) => {
          this.creating.set(false);
          const id = (resp.body as any)?.id;
          if (id) {
            this.toast.success('Execução criada');
            this.load(id);
          } else {
            this.toast.info('Criado, mas não recebi o ID. Recarregue a página.');
          }
        },
        error: (e) => {
          this.creating.set(false);

          if (e?.status === 409) {
            const idFromBody = e?.error?.existing?.id as string | undefined;
            const idFromHeader =
              e?.headers?.get?.('X-Existing-Execution-Id') ??
              this.idFromLocation(e?.headers?.get?.('Location'));

            const id = idFromBody || idFromHeader;

            this.toast.info('Já existe uma execução ativa. Carreguei ela pra você.');
            if (id) {
              this.load(id);
            } else {
              this.toast.error(
                'Conflito detectado, mas não consegui identificar o ID da execução existente.'
              );
            }
            return;
          }

          const msg = this.msgFromErr(e, 'Erro ao criar execução');
          this.toast.error(msg);
          this.error.set(msg);
        },
      });
  }

  load(id: string, opts?: { silent?: boolean }) {
    if (!opts?.silent) this.loadingExec.set(true);
    this.error.set(null);

    this.api.getExecution(id).subscribe({
      next: (ex) => {
        this.exec.set(ex);
        if (!opts?.silent) this.loadingExec.set(false);
        this.loadTemplateItems(ex.templateId);
      },
      error: (e) => {
        if (!opts?.silent) this.loadingExec.set(false);
        const msg = this.msgFromErr(e, 'Erro ao carregar');
        this.toast.error(msg);
        this.error.set(msg);
      },
    });
  }

  start() {
    const ex = this.exec();
    if (!ex) return;
    this.starting.set(true);
    this.error.set(null);
    this.api.startExecution(ex.id, this.executorId).subscribe({
      next: () => {
        this.starting.set(false);
        this.toast.success('Execução iniciada');
        this.load(ex.id);
      },
      error: (e) => {
        this.starting.set(false);
        this.toast.error('Erro ao iniciar');
        this.error.set(e?.message ?? 'Erro ao iniciar');
      },
    });
  }

  patch(itemId: string, status: number, observation: string | null) {
    const ex = this.exec();
    if (!ex) return;

    const item = ex.items.find((i) => i.templateItemId === itemId);
    if (!item) {
      this.toast.error('Item não encontrado na execução');
      return;
    }

    this.patchingId.set(itemId);
    this.error.set(null);

    this.api
      .patchItem(
        ex.id,
        itemId,
        { status, observation, rowVersion: item.rowVersion },
        this.executorId
      )
      .subscribe({
        next: () => {
          this.patchingId.set(null);
          this.toast.info('Item atualizado');
          this.load(ex.id, { silent: true });
        },
        error: (e) => {
          this.patchingId.set(null);

          if (this.shouldReload(e)) {
            const msg = this.msgFromErr(e, 'Conflito / estado inválido. Atualizei a tela.');
            this.toast.info(msg);
            this.load(ex.id, { silent: true });
            return;
          }

          const msg = this.msgFromErr(e, 'Erro ao salvar item');
          this.toast.error(msg);
          this.error.set(msg);
        },
      });
  }

  submit() {
    const ex = this.exec();
    if (!ex) return;

    this.submitting.set(true);
    this.error.set(null);

    this.api.submitExecution(ex.id, ex.rowVersion, this.executorId).subscribe({
      next: () => {
        this.submitting.set(false);
        this.toast.success('Execução enviada para aprovação');
        this.load(ex.id, { silent: true });
      },
      error: (e) => {
        this.submitting.set(false);

        if (this.shouldReload(e)) {
          const msg = this.msgFromErr(e, 'Conflito / estado mudou. Atualizei a tela.');
          this.toast.info(msg);
          this.load(ex.id, { silent: true });
          return;
        }

        const msg = this.msgFromErr(e, 'Erro ao enviar');
        this.toast.error(msg);
        this.error.set(msg);
      },
    });
  }

  approve(decision: 0 | 1) {
    const ex = this.exec();
    if (!ex) return;

    this.approving.set(decision);
    this.error.set(null);

    this.api
      .approveExecution(
        ex.id,
        decision,
        decision === 0 ? 'OK' : 'Reprovado',
        ex.rowVersion,
        this.supervisorId
      )
      .subscribe({
        next: () => {
          this.approving.set(null);
          this.toast.success(decision === 0 ? 'Aprovado' : 'Reprovado');
          this.load(ex.id, { silent: true });
        },
        error: (e) => {
          this.approving.set(null);

          if (this.shouldReload(e)) {
            const msg = this.msgFromErr(e, 'Conflito / estado mudou. Atualizei a tela.');
            this.toast.info(msg);
            this.load(ex.id, { silent: true });
            return;
          }

          const msg = this.msgFromErr(e, 'Erro na decisão');
          this.toast.error(msg);
          this.error.set(msg);
        },
      });
  }

  statusLabel(s: number): string {
    switch (s) {
      case 0:
        return 'Draft';
      case 1:
        return 'InProgress';
      case 2:
        return 'Submitted';
      case 3:
        return 'Approved';
      case 4:
        return 'Rejected';
      default:
        return String(s);
    }
  }

  templateName(id: string): string {
    const t = this.templates().find((x) => x.id === id);
    return t ? t.name : id;
  }

  vehicleText(id: string): string {
    const v = this.vehicles().find((x) => x.id === id);
    return v ? `${v.plate} — ${v.model}` : id;
  }
  itemLabel(id: string) {
    return this.templateItems().find((i) => i.id === id)?.label || id;
  }
  itemRequired(id: string) {
    return !!this.templateItems().find((i) => i.id === id)?.required;
  }
}
